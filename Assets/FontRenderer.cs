using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Typography.Contours;
using Typography.OpenFont;
using UnityEngine;
using Unity.VectorGraphics;


class VectorGraphicsGlyphTranslator : IGlyphTranslator
{
    private List<BezierContour> _contours;
    private List<BezierSegment> _currentSegments;
    public BezierContour[] Result => this._contours.ToArray();

    private float lastX;
    private float lastY;
    private float lastMoveX;
    private float lastMoveY;
    private bool isClosed;

    public void BeginRead(int contourCount)
    {
        _contours = new();
        _currentSegments = new List<BezierSegment>();
        Debug.Log($"count {contourCount}");
    }

    public void EndRead()
    {
        Debug.Log("End");
        _contours.Add(new BezierContour()
        {
            Closed = isClosed,
            Segments = VectorUtils.BezierSegmentsToPath(_currentSegments.ToArray()),
        });
    }

    public void MoveTo(float x0, float y0)
    {
        Debug.Log($"Move to {x0}, {y0}");
        if (isClosed)
        {
            _contours.Add(new BezierContour()
            {
                Closed = isClosed,
                Segments = VectorUtils.BezierSegmentsToPath(_currentSegments.ToArray()),
            });
            _currentSegments = new List<BezierSegment>();
        }

        lastX = lastMoveX = (float) x0;
        lastY = lastMoveY = (float) y0;
    }

    public void LineTo(float x1, float y1)
    {
        Debug.Log($"Line to {x1}, {y1}");
        isClosed = false;

        var p0 = new Vector2() {x = lastX, y = lastY};
        var p2 = new Vector2() {x = x1, y = y1};

        var line = VectorUtils.MakeLine(p0, p2);
        lastX = x1;
        lastY = y1;
        _currentSegments.Add(line);
    }

    public void Curve3(float x1, float y1, float x2, float y2)
    {
        Debug.Log($"curve 3 to {x1}, {y1}, {x2}, {y2}");
        isClosed = false;

        // Turn Quadratic curve to Cubic curve
        float c1x = lastX + (float) ((2f / 3f) * (x1 - lastX));
        float c1y = lastY + (float) ((2f / 3f) * (y1 - lastY));
        //---------------------------------------------------------------------
        float c2x = (float) (x2 + ((2f / 3f) * (x1 - x2)));
        float c2y = (float) (y2 + ((2f / 3f) * (y1 - y2)));
        //---------------------------------------------------------------------

        var bezier = new BezierSegment()
        {
            P0 = new Vector2()
            {
                x = lastX,
                y = lastY,
            },
            P1 = new Vector2()
            {
                x = c1x,
                y = c1y,
            },
            P2 = new Vector2()
            {
                x = c2x,
                y = c2y,
            },
            P3 = new Vector2()
            {
                x = x2,
                y = y2,
            },
        };
        lastX = (float) x2;
        lastY = (float) y2;
        _currentSegments.Add(bezier);
    }

    public void Curve4(float x1, float y1, float x2, float y2, float x3, float y3)
    {
        Debug.Log($"curve 4 to {x1}, {y1}, {x2}, {y2}, {x3} {y3}");
        isClosed = false;
        var bezier = new BezierSegment()
        {
            P0 = new Vector2()
            {
                x = lastX,
                y = lastY,
            },
            P1 = new Vector2()
            {
                x = x1,
                y = y1,
            },
            P2 = new Vector2()
            {
                x = x2,
                y = y2,
            },
            P3 = new Vector2()
            {
                x = lastX = (float) x3,
                y = lastY = (float) y3,
            },
        };

        _currentSegments.Add(bezier);
    }

    public void CloseContour()
    {
        Debug.Log("close contour");
        isClosed = true;
        CloseContourWithStraightLine();
        _contours.Add(new BezierContour()
        {
            Closed = isClosed,
            Segments = VectorUtils.BezierSegmentsToPath(_currentSegments.ToArray()),
        });
        _currentSegments = new List<BezierSegment>();

        lastX = lastMoveX;
        lastY = lastMoveY;
    }

    private void CloseContourWithStraightLine()
    {
        var firstSegment = _currentSegments[0];
        var lastSegment = _currentSegments[^1];
        var line = VectorUtils.MakeLine(lastSegment.P3, firstSegment.P0);
        _currentSegments.Add(line);
        lastX = firstSegment.P0.x;
        lastY = firstSegment.P0.y;
    }
}

public class FontRenderer : MonoBehaviour
{
    private Typeface _typeface;

    [SerializeField] private string GlyphName;

    // Start is called before the first frame update
    void Start()
    {
        var fontPath = System.IO.Path.Combine(Application.dataPath, "Polihymnia.ttf");
        using (FileStream fs = new FileStream(fontPath, FileMode.Open))
        {
            OpenFontReader fontReader = new OpenFontReader();
            _typeface = fontReader.Read(fs);
        }

        Debug.Log($"Number of glyph {_typeface.GlyphCount}");

        var glyphA = _typeface.GetGlyphByName(GlyphName);
        var glyphB = _typeface.GetGlyphByName("nine");
        Debug.Log($"Glyph index {glyphA.GlyphIndex}");

        var points = glyphA.GlyphPoints;


        var builder = new GlyphOutlineBuilder(_typeface);
        var fontsize = 40;
        builder.BuildFromGlyph(glyphA, fontsize);
        var tx = new VectorGraphicsGlyphTranslator();
        builder.ReadShapes(tx);

        var aHeight = ((glyphA.MaxY * fontsize) - (glyphA.MinY * fontsize)) / fontsize;
        var aWidth = ((glyphA.MaxX * fontsize) - (glyphA.MinX * fontsize)) / fontsize;
        

        var txb = new VectorGraphicsGlyphTranslator();
        builder.BuildFromGlyph(glyphB, fontsize);
        builder.ReadShapes(txb);

        var scene = new Scene()
        {
            Root = new SceneNode()
            {
                Shapes = new List<Shape>()
                {
                    new Shape()
                    {
                        Contours = new[]
                        {
                            new BezierContour()
                            {
                                Segments = VectorUtils.MakePathLine(new Vector2(-(aWidth / 2.0f), 0), new Vector2((aWidth / 2.0f), 0)),
                                Closed = false,
                            },
                            new BezierContour()
                            {
                                Segments = VectorUtils.MakePathLine(new Vector2(0, (-aHeight) / 2.0f), new Vector2(0, aHeight / 2.0f)),
                                Closed = false,
                            },
                        },
                        Fill = new SolidFill()
                        {
                            Color = Color.black,
                        },
                        PathProps = new PathProperties()
                        {
                            Stroke = new Stroke()
                            {
                                Color = Color.black,
                                HalfThickness = 0.5f
                            }
                        }
                    },
                    new Shape()
                    {
                        Contours = tx.Result,
                        Fill = new SolidFill()
                        {
                            Color = Color.black,
                        },
                        PathProps = new PathProperties()
                        {
                            Stroke = new Stroke()
                            {
                                Color = Color.black,
                                HalfThickness = 0.5f,
                            },
                        },
                    },
                    // new Shape()
                    // {
                    //     Contours = txb.Result,
                    //     Fill = new SolidFill()
                    //     {
                    //         Color = Color.black,
                    //     },
                    //     PathProps = new PathProperties()
                    //     {
                    //         Stroke = new Stroke()
                    //         {
                    //             Color = Color.black,
                    //             HalfThickness = 0.5f,
                    //         },
                    //     },
                    // },
                }
            }
        };

        var geoms = VectorUtils.TessellateScene(scene, tessOptions);
        var sprite = VectorUtils.BuildSprite(geoms, 50.0f, VectorUtils.Alignment.Center, Vector2.zero, 16, false);

        GetComponent<SpriteRenderer>().sprite = sprite;
    }

    // Update is called once per frame
    void Update()
    {
    }

    private VectorUtils.TessellationOptions tessOptions = new VectorUtils.TessellationOptions()
    {
        StepDistance = 100.0f,
        MaxCordDeviation = 0.5f,
        MaxTanAngleDeviation = 0.1f,
        SamplingStepSize = 0.01f
    };
}