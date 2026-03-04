using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Quest3VR.Prototype;

namespace Quest3VR.NPH
{
    public class NPHScorePanel : MonoBehaviour
    {
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI recommendationText;
        [SerializeField] private Image scoreBar;

        [Header("Metrics Breakdown")]
        [SerializeField] private TextMeshProUGUI evansIndexText;
        [SerializeField] private TextMeshProUGUI vsrText;
        [SerializeField] private TextMeshProUGUI callosalAngleText;
        [SerializeField] private TextMeshProUGUI deshScoreText;
        [SerializeField] private TextMeshProUGUI sylvianText;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI connectionStatusText;
        [SerializeField] private TextMeshProUGUI analysisStatusText;

        public static NPHScorePanel Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        public void DisplayScore(ScoreResponse score)
        {
            if (scoreText != null)
                scoreText.text = $"{score.score}";

            if (labelText != null)
            {
                labelText.text = score.label;
                if (ColorUtility.TryParseHtmlString(score.color, out Color c))
                    labelText.color = c;
            }

            if (recommendationText != null)
                recommendationText.text = score.recommendation;

            if (scoreBar != null)
            {
                scoreBar.fillAmount = score.score / 100f;
                if (ColorUtility.TryParseHtmlString(score.color, out Color barColor))
                    scoreBar.color = barColor;
            }

            if (score.score >= 75)
                VRHapticsManager.Instance?.Warning(VRHapticsManager.ControllerType.Both);
            else if (score.score >= 50)
                VRHapticsManager.Instance?.DoublePulse(VRHapticsManager.ControllerType.Both);
            else
                VRHapticsManager.Instance?.Tap(VRHapticsManager.ControllerType.Both);

            VRDashboardController.Instance?.UpdateStatus($"NPH Score: {score.score} — {score.label}");
        }

        public void DisplayMetrics(NPHMetrics metrics)
        {
            if (evansIndexText != null)
                evansIndexText.text = $"Evans Index: {metrics.evans_index:F3}";

            if (vsrText != null)
                vsrText.text = metrics.vsr.HasValue
                    ? $"VSR: {metrics.vsr.Value:F3}"
                    : "VSR: N/A (2D only)";

            if (callosalAngleText != null)
                callosalAngleText.text = metrics.callosal_angle.HasValue
                    ? $"Callosal Angle: {metrics.callosal_angle.Value:F1}°"
                    : "Callosal Angle: N/A";

            if (deshScoreText != null)
                deshScoreText.text = metrics.desh_score.HasValue
                    ? $"DESH Score: {metrics.desh_score.Value}/3"
                    : "DESH Score: N/A";

            if (sylvianText != null)
                sylvianText.text = metrics.sylvian_dilation.HasValue
                    ? $"Sylvian Dilation: {(metrics.sylvian_dilation.Value ? "Yes" : "No")}"
                    : "Sylvian Dilation: N/A";
        }

        public void SetConnectionStatus(bool connected, bool modelLoaded)
        {
            if (connectionStatusText != null)
            {
                connectionStatusText.text = connected
                    ? (modelLoaded ? "Backend: Connected (YOLO loaded)" : "Backend: Connected (demo mode)")
                    : "Backend: Disconnected";
                connectionStatusText.color = connected ? Color.green : Color.red;
            }
        }

        public void SetAnalysisStatus(string status)
        {
            if (analysisStatusText != null)
                analysisStatusText.text = status;
        }
    }
}
