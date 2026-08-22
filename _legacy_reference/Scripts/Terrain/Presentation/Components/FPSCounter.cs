namespace ProjectTwo.Terrain.Presentation.Components
{
    using System;
    using UnityEngine;
    using UnityEngine.Profiling;
    using UnityEngine.InputSystem;

    /// <summary>
    /// Lightweight performance overlay displaying real-time FPS, frame timing, memory usage, and terrain streaming stats.
    /// Toggle visibility with F3 key.
    /// Fully compatible with the modern Unity Input System package.
    /// </summary>
    [AddComponentMenu("Terrain/Performance/FPS Counter Overlay")]
    public class FPSCounter : MonoBehaviour
    {
        public enum OverlayAnchor
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        [Header("Display Settings")]
        [Tooltip("Screen anchor position for the performance overlay.")]
        public OverlayAnchor Anchor = OverlayAnchor.TopRight;

        [Tooltip("Update frequency in seconds for smoothed statistics.")]
        [Range(0.05f, 1f)]
        public float UpdateInterval = 0.2f;

        [Tooltip("Show memory allocation statistics.")]
        public bool ShowMemoryStats = true;

        [Tooltip("Show active terrain chunk count if TerrainGenerator is present.")]
        public bool ShowTerrainStats = true;

        [Tooltip("Toggle overlay visibility with key press.")]
        public bool EnableToggleHotkey = true;

        private float _accumulatedTime;
        private int _accumulatedFrames;
        private float _timeUntilNextUpdate;

        private float _currentFPS;
        private float _currentFrameTimeMs;
        private float _minFPS = float.MaxValue;
        private float _maxFPS = 0f;

        private float _allocatedMemoryMb;
        private bool _isVisible = true;

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _fpsStyle;

        private TerrainGenerator _cachedGenerator;

        private void Start()
        {
            _timeUntilNextUpdate = UpdateInterval;
            _cachedGenerator = FindAnyObjectByType<TerrainGenerator>();
        }

        private void Update()
        {
            if (EnableToggleHotkey)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.f3Key.wasPressedThisFrame)
                {
                    _isVisible = !_isVisible;
                }
            }

            float dt = Time.unscaledDeltaTime;
            _accumulatedTime += dt;
            _accumulatedFrames++;
            _timeUntilNextUpdate -= dt;

            if (_timeUntilNextUpdate <= 0f)
            {
                if (_accumulatedTime > 0f)
                {
                    _currentFPS = _accumulatedFrames / _accumulatedTime;
                    _currentFrameTimeMs = (_accumulatedTime / _accumulatedFrames) * 1000f;

                    if (_currentFPS < _minFPS) _minFPS = _currentFPS;
                    if (_currentFPS > _maxFPS) _maxFPS = _currentFPS;
                }

                if (ShowMemoryStats)
                {
                    _allocatedMemoryMb = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
                }

                _accumulatedTime = 0f;
                _accumulatedFrames = 0;
                _timeUntilNextUpdate = UpdateInterval;
            }
        }

        private void OnGUI()
        {
            if (!_isVisible) return;

            InitializeStyles();

            float width = 230f;
            float height = 115f;
            if (ShowTerrainStats && _cachedGenerator != null) height += 35f;
            if (ShowMemoryStats) height += 20f;

            float margin = 16f;
            float posX = margin;
            float posY = margin;

            switch (Anchor)
            {
                case OverlayAnchor.TopRight:
                    posX = Screen.width - width - margin;
                    posY = margin;
                    break;
                case OverlayAnchor.BottomLeft:
                    posX = margin;
                    posY = Screen.height - height - margin;
                    break;
                case OverlayAnchor.BottomRight:
                    posX = Screen.width - width - margin;
                    posY = Screen.height - height - margin;
                    break;
                case OverlayAnchor.TopLeft:
                default:
                    posX = margin;
                    posY = margin;
                    break;
            }

            Rect boxRect = new Rect(posX, posY, width, height);

            GUI.Box(boxRect, GUIContent.none, _boxStyle);

            GUILayout.BeginArea(new Rect(posX + 10f, posY + 8f, width - 20f, height - 16f));

            // Dynamic FPS Color (Green >= 60, Yellow 30-59, Red < 30)
            Color fpsColor = _currentFPS >= 60f
                ? new Color(0.18f, 0.8f, 0.44f) // Emerald Green
                : (_currentFPS >= 30f
                    ? new Color(0.95f, 0.77f, 0.06f) // Amber Yellow
                    : new Color(0.91f, 0.3f, 0.24f)); // Coral Red

            _fpsStyle.normal.textColor = fpsColor;

            GUILayout.Label($"{_currentFPS:0} FPS  <color=#A0A0A0>({_currentFrameTimeMs:0.0} ms)</color>", _fpsStyle);

            GUILayout.Space(2f);
            GUILayout.Label($"Min: {_minFPS:0}  |  Max: {_maxFPS:0}", _labelStyle);

            if (ShowMemoryStats)
            {
                GUILayout.Label($"Allocated RAM: {_allocatedMemoryMb:0.0} MB", _labelStyle);
            }

            if (ShowTerrainStats && _cachedGenerator != null && _cachedGenerator.Configuration != null)
            {
                GUILayout.Space(2f);
                GUILayout.Label($"Terrain View Radius: {_cachedGenerator.Configuration.MaxViewDistance:0} m", _labelStyle);
                GUILayout.Label($"Chunk Size: {_cachedGenerator.Configuration.ChunkSize} m", _labelStyle);
            }

            GUILayout.Label("<color=#707070>[F3] Toggle Overlay</color>", _labelStyle);

            GUILayout.EndArea();
        }

        private void InitializeStyles()
        {
            if (_boxStyle == null)
            {
                Texture2D bgTex = new Texture2D(1, 1);
                bgTex.SetPixel(0, 0, new Color(0.08f, 0.10f, 0.14f, 0.85f));
                bgTex.Apply();

                _boxStyle = new GUIStyle(GUI.skin.box);
                _boxStyle.normal.background = bgTex;
            }

            if (_fpsStyle == null)
            {
                _fpsStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    richText = true
                };
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Normal,
                    richText = true
                };
                _labelStyle.normal.textColor = new Color(0.85f, 0.88f, 0.92f);
            }
        }
    }
}
