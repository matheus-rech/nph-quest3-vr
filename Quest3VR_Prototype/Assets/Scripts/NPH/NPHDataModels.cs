using System;
using System.Collections.Generic;
using UnityEngine;

namespace Quest3VR.NPH
{
    [Serializable]
    public class DetectionBox
    {
        public string @class;
        public int x1, y1, x2, y2;
        public float confidence;
    }

    [Serializable]
    public class NPHMetrics
    {
        public float evans_index;
        public float? callosal_angle;
        public int? desh_score;
        public bool? sylvian_dilation;
        public float? vsr;
        public bool? periventricular_changes;
        public string cortical_atrophy;
        public float nph_probability;
        public int? evans_slice;
    }

    [Serializable]
    public class AnalyzeResponse
    {
        public List<DetectionBox> boxes;
        public NPHMetrics metrics;
        public int image_width;
        public int image_height;
        public bool demo_mode;
    }

    [Serializable]
    public class AnalyzeCT3DResponse
    {
        public NPHMetrics metrics;
        public string source;
        public bool demo_mode;
    }

    [Serializable]
    public class ScoreRequest
    {
        public float evansIndex;
        public float? callosalAngle;
        public int deshScore;
        public bool sylvianDilation;
        public float? vsr;
        public List<bool> triad;
        public string corticalAtrophy;
    }

    [Serializable]
    public class ScoreResponse
    {
        public int score;
        public string label;
        public string color;
        public string recommendation;
    }

    [Serializable]
    public class HealthResponse
    {
        public string status;
        public string yolo_model;
        public bool model_loaded;
    }

    public static class NPHClassColors
    {
        public static readonly Dictionary<string, Color> Map = new()
        {
            { "ventricle",        new Color(0.2f, 0.6f, 1.0f, 0.8f) },
            { "sylvian_fissure",  new Color(1.0f, 0.8f, 0.2f, 0.8f) },
            { "tight_convexity",  new Color(0.2f, 1.0f, 0.4f, 0.8f) },
            { "pvh",              new Color(1.0f, 0.3f, 0.3f, 0.8f) },
            { "skull_inner",      new Color(0.7f, 0.7f, 0.7f, 0.5f) },
        };

        public static Color Get(string className)
        {
            return Map.TryGetValue(className, out var c) ? c : Color.white;
        }
    }
}
