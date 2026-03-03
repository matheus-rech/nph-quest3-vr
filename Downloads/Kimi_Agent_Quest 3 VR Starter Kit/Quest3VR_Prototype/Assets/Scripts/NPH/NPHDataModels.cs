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

    [Serializable]
    public class MeshVentricleResponse
    {
        public string status;
        public int vertex_count;
        public int triangle_count;
        public float[] vertices;      // Flat array: [x1, y1, z1, x2, y2, z2, ...]
        public int[] triangles;       // Triangle indices
        public float[] normals;       // Optional vertex normals
        public float volume_cc;       // Estimated ventricle volume in cubic centimeters
        public NPHMetrics metrics;    // Associated metrics
    }

    /// <summary>
    /// Demo mode data generator for when backend is unavailable.
    /// </summary>
    public static class DemoModeData
    {
        private static readonly System.Random random = new System.Random();

        /// <summary>
        /// Generate synthetic metrics for demo purposes.
        /// </summary>
        public static NPHMetrics GenerateSyntheticMetrics(float evansIndexOverride = -1f)
        {
            float evansIndex = evansIndexOverride > 0 
                ? evansIndexOverride 
                : 0.25f + (float)random.NextDouble() * 0.15f; // 0.25 - 0.40

            return new NPHMetrics
            {
                evans_index = evansIndex,
                vsr = 1.0f + evansIndex * 4f + (float)random.NextDouble() * 0.5f,
                callosal_angle = 60f + (float)random.NextDouble() * 60f, // 60-120 degrees
                desh_score = random.Next(0, 4),
                sylvian_dilation = random.NextDouble() > 0.5,
                periventricular_changes = random.NextDouble() > 0.7,
                cortical_atrophy = random.NextDouble() > 0.6 ? "mild" : "none",
                nph_probability = Mathf.Clamp01((evansIndex - 0.25f) * 3f + (float)random.NextDouble() * 0.2f),
                evans_slice = random.Next(50, 150)
            };
        }

        /// <summary>
        /// Generate synthetic detection boxes for demo purposes.
        /// </summary>
        public static List<DetectionBox> GenerateSyntheticBoxes(int imageWidth = 512, int imageHeight = 512)
        {
            var boxes = new List<DetectionBox>();
            int centerX = imageWidth / 2;
            int centerY = imageHeight / 2;

            // Skull inner (large circle)
            int skullRadius = (int)(imageWidth * 0.38f);
            boxes.Add(new DetectionBox
            {
                @class = "skull_inner",
                x1 = centerX - skullRadius,
                y1 = centerY - skullRadius,
                x2 = centerX + skullRadius,
                y2 = centerY + skullRadius,
                confidence = 0.95f
            });

            // Left ventricle
            int ventWidth = (int)(imageWidth * 0.12f);
            int ventHeight = (int)(imageHeight * 0.15f);
            boxes.Add(new DetectionBox
            {
                @class = "ventricle",
                x1 = centerX - (int)(imageWidth * 0.12f) - ventWidth,
                y1 = centerY - ventHeight / 2,
                x2 = centerX - (int)(imageWidth * 0.12f),
                y2 = centerY + ventHeight / 2,
                confidence = 0.88f
            });

            // Right ventricle
            boxes.Add(new DetectionBox
            {
                @class = "ventricle",
                x1 = centerX + (int)(imageWidth * 0.12f),
                y1 = centerY - ventHeight / 2,
                x2 = centerX + (int)(imageWidth * 0.12f) + ventWidth,
                y2 = centerY + ventHeight / 2,
                confidence = 0.86f
            });

            // Random chance for sylvian fissure
            if (random.NextDouble() > 0.3)
            {
                boxes.Add(new DetectionBox
                {
                    @class = "sylvian_fissure",
                    x1 = centerX - (int)(imageWidth * 0.25f),
                    y1 = centerY + (int)(imageHeight * 0.1f),
                    x2 = centerX + (int)(imageWidth * 0.25f),
                    y2 = centerY + (int)(imageHeight * 0.25f),
                    confidence = 0.72f
                });
            }

            return boxes;
        }

        /// <summary>
        /// Generate a complete synthetic analyze response.
        /// </summary>
        public static AnalyzeResponse GenerateSyntheticAnalyzeResponse(int imageWidth = 512, int imageHeight = 512)
        {
            var metrics = GenerateSyntheticMetrics();
            return new AnalyzeResponse
            {
                boxes = GenerateSyntheticBoxes(imageWidth, imageHeight),
                metrics = metrics,
                image_width = imageWidth,
                image_height = imageHeight,
                demo_mode = true
            };
        }

        /// <summary>
        /// Calculate a score from metrics for demo mode.
        /// </summary>
        public static ScoreResponse CalculateDemoScore(NPHMetrics metrics)
        {
            // Weighted scoring: VSR 40% + Evans 25% + Callosal 20% + DESH 10% + Sylvian 5%
            float vsrScore = metrics.vsr.HasValue ? Mathf.Clamp01((metrics.vsr.Value - 0.5f) / 2.5f) * 40f : 0f;
            float evansScore = Mathf.Clamp01((metrics.evans_index - 0.2f) / 0.3f) * 25f;
            float callosalScore = metrics.callosal_angle.HasValue 
                ? Mathf.Clamp01((120f - metrics.callosal_angle.Value) / 60f) * 20f 
                : 10f;
            float deshScore = metrics.desh_score.HasValue 
                ? (metrics.desh_score.Value / 3f) * 10f 
                : 5f;
            float sylvianScore = metrics.sylvian_dilation.HasValue && metrics.sylvian_dilation.Value ? 5f : 0f;

            int totalScore = Mathf.RoundToInt(vsrScore + evansScore + callosalScore + deshScore + sylvianScore);
            totalScore = Mathf.Clamp(totalScore, 0, 100);

            string label, color, recommendation;
            if (totalScore >= 75)
            {
                label = "High Probability";
                color = "#FF4444";
                recommendation = "Strong NPH indicators present. Recommend shunt evaluation.";
            }
            else if (totalScore >= 50)
            {
                label = "Moderate Probability";
                color = "#FFAA00";
                recommendation = "Some NPH characteristics observed. Consider further assessment.";
            }
            else if (totalScore >= 25)
            {
                label = "Low Probability";
                color = "#FFFF00";
                recommendation = "Mild ventricular changes. Monitor for symptom progression.";
            }
            else
            {
                label = "Very Low Probability";
                color = "#44FF44";
                recommendation = "No significant NPH indicators detected.";
            }

            return new ScoreResponse
            {
                score = totalScore,
                label = label,
                color = color,
                recommendation = recommendation
            };
        }
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
