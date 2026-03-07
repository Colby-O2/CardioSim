using UnityEditor;
using UnityEngine;

using ColbyO.CardioSim.Rendering;

//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    HeartEditor.cs
//-----------------------------------------------------------------------
namespace ColbyO.CardioSim.Editor
{
    /// <summary>
    /// Custom inspector for the Heart component.
    /// </summary>
    [CustomEditor(typeof(ECGVisualizer))]
    public class ECGVisualizerEditor : UnityEditor.Editor
    {
        private SerializedProperty _displayModeProp;

        private SerializedProperty _maxSampleBufferProp;
        private SerializedProperty _secondsToDisplayProp;
        private SerializedProperty _yScaleProp;
        private SerializedProperty _totalWidthProp;
        private SerializedProperty _leadLengthProp;
        private SerializedProperty _persistenceProp;
        private SerializedProperty _traceColorProp;
        private SerializedProperty _flashColorProp;
        private SerializedProperty _lineWidthProp;

        private SerializedProperty _ecgMaterialProp;
        private SerializedProperty _meshRendererProp;
        private SerializedProperty _customRendererProp;

        private SerializedProperty _backgroundColorProp;
        private SerializedProperty _enableGridProp;
        private SerializedProperty _gridColorProp;
        private SerializedProperty _gridSizeProp;
        private SerializedProperty _gridLineThicknessProp;
        private SerializedProperty _enableGridFadeOutProp;

        private void OnEnable()
        {
            _displayModeProp = serializedObject.FindProperty("_displayMode");

            _maxSampleBufferProp = serializedObject.FindProperty("_maxSampleBuffer");
            _secondsToDisplayProp = serializedObject.FindProperty("_secondsToDisplay");
            _yScaleProp = serializedObject.FindProperty("_yScale");
            _totalWidthProp = serializedObject.FindProperty("_totalWidth");
            _leadLengthProp = serializedObject.FindProperty("_leadLength");
            _persistenceProp = serializedObject.FindProperty("_persistence");
            _traceColorProp = serializedObject.FindProperty("_traceColor");
            _flashColorProp = serializedObject.FindProperty("_flashColor");
            _lineWidthProp = serializedObject.FindProperty("_lineWidth");

            _ecgMaterialProp = serializedObject.FindProperty("_ecgMaterial");
            _meshRendererProp = serializedObject.FindProperty("_meshRenderer");
            _customRendererProp = serializedObject.FindProperty("_rendererComponent");

            _backgroundColorProp = serializedObject.FindProperty("_backgroundColor");
            _enableGridProp = serializedObject.FindProperty("_enableGrid");
            _gridColorProp = serializedObject.FindProperty("_gridColor");
            _gridSizeProp = serializedObject.FindProperty("_gridSize");
            _gridLineThicknessProp = serializedObject.FindProperty("_gridLineThickness");
            _enableGridFadeOutProp = serializedObject.FindProperty("_enableGridFadeOut");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ECGVisualizer visualizer = (ECGVisualizer)target;

            DrawDefaultInspectorExceptDisplay();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rendering Configuration", EditorStyles.boldLabel);

            ECGDisplayMode currentDisplayMode = (ECGDisplayMode)_displayModeProp.enumValueIndex;
            ECGDisplayMode newDisplayMode = (ECGDisplayMode)EditorGUILayout.EnumPopup("Display Mode", currentDisplayMode);
            _displayModeProp.enumValueIndex = (int)newDisplayMode;

            if (newDisplayMode == ECGDisplayMode.Mesh || newDisplayMode == ECGDisplayMode.Material)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("ECG Display Settings", EditorStyles.boldLabel);

                _maxSampleBufferProp.intValue = EditorGUILayout.IntSlider("Resolution", _maxSampleBufferProp.intValue, 128, 8192);

                _secondsToDisplayProp.floatValue = EditorGUILayout.FloatField("Seconds to Display", _secondsToDisplayProp.floatValue);
                if (newDisplayMode == ECGDisplayMode.Mesh) _yScaleProp.floatValue = EditorGUILayout.FloatField("Y Amplitude Scale", _yScaleProp.floatValue);
                else _yScaleProp.floatValue = EditorGUILayout.Slider("Y Amplitude Scale", _yScaleProp.floatValue, 0f, 1f);
                _lineWidthProp.floatValue = EditorGUILayout.Slider("Line Width", _lineWidthProp.floatValue, 0.0001f, 0.02f);
                _leadLengthProp.intValue = EditorGUILayout.IntSlider("Lead Length", _leadLengthProp.intValue, 1, 20);
                _persistenceProp.floatValue = EditorGUILayout.Slider("Persistence", _persistenceProp.floatValue, 0.8f, 1f);

                _traceColorProp.colorValue = EditorGUILayout.ColorField(new GUIContent("Trace Color"), _traceColorProp.colorValue, showEyedropper: true, showAlpha: false, hdr: true);
                _flashColorProp.colorValue = EditorGUILayout.ColorField(new GUIContent("Lead Color"), _flashColorProp.colorValue, showEyedropper: true, showAlpha: false, hdr: true);

                if (newDisplayMode == ECGDisplayMode.Mesh)
                {
                    _totalWidthProp.floatValue = EditorGUILayout.FloatField("Physical Width", _totalWidthProp.floatValue);

                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("Using CPU-based rendering backend.", MessageType.Info);
                    _meshRendererProp.objectReferenceValue = (MeshRenderer)EditorGUILayout.ObjectField("Mesh Renderer", _meshRendererProp.objectReferenceValue, typeof(MeshRenderer), true);
                }
                else
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Background Settings", EditorStyles.boldLabel);
                    _backgroundColorProp.colorValue = EditorGUILayout.ColorField(new GUIContent("Background Color"), _backgroundColorProp.colorValue, showEyedropper: true, showAlpha: true, hdr: false);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
                    _enableGridProp.boolValue = EditorGUILayout.Toggle(new GUIContent("Enable Grid"), _enableGridProp.boolValue);
                    if (_enableGridProp.boolValue)
                    {
                        _gridColorProp.colorValue = EditorGUILayout.ColorField(new GUIContent("Grid Color"), _gridColorProp.colorValue, showEyedropper: true, showAlpha: false, hdr: false);
                        _gridSizeProp.floatValue = EditorGUILayout.Slider(new GUIContent("Grid Size"), _gridSizeProp.floatValue, 10f, 100f);
                        _gridLineThicknessProp.floatValue = EditorGUILayout.Slider(new GUIContent("Grid Line Thickness"), _gridLineThicknessProp.floatValue, 0.0f, 1.0f);
                        _enableGridFadeOutProp.boolValue = EditorGUILayout.Toggle(new GUIContent("Enable Grid Fade Out"), _enableGridFadeOutProp.boolValue);
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("Using GPU-based rendering backend.", MessageType.Info);
                    _ecgMaterialProp.objectReferenceValue = (Material)EditorGUILayout.ObjectField("ECG Material", _ecgMaterialProp.objectReferenceValue, typeof(Material), false);
                }
            }
            else if (newDisplayMode == ECGDisplayMode.Custom)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Using custom rendering backend.", MessageType.Info);
                Object assignedObject = EditorGUILayout.ObjectField(
                    "Renderer",
                    _customRendererProp.objectReferenceValue,
                    typeof(MonoBehaviour),
                    true
                );

                if (assignedObject is IECGRenderer)
                {
                    _customRendererProp.objectReferenceValue = assignedObject as MonoBehaviour;
                }
                else
                {
                    _customRendererProp.objectReferenceValue = null;
                }
 
            }
            else
            {
                EditorGUILayout.HelpBox("No rendering backend selected.", MessageType.Info);
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(visualizer);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDefaultInspectorExceptDisplay()
        {
            DrawPropertiesExcluding(serializedObject, new string[] { "displayMode", "secondsToDisplay", "yScale", "ecgMaterial", "textureResolution", "meshRenderer" });
            serializedObject.ApplyModifiedProperties();
        }
    }
}
