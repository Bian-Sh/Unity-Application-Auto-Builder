using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace zFramework.AppBuilder
{
    public class PresetReceiver : PresetSelectorReceiver
    {
        private Object m_Target;
        private Preset m_InitialValue;
        private AppAutoBuilderSettingProvider provider;

        internal void Init(Object target, AppAutoBuilderSettingProvider provider)
        {
            m_Target = target;
            this.provider = provider;
            m_InitialValue = new Preset(target);
        }
        public override void OnSelectionChanged(Preset selection)
        {
            if (selection != null)
            {
                Undo.RecordObject(m_Target, "Apply Preset " + selection.name);
                selection.ApplyTo(m_Target);
            }
            else
            {
                Undo.RecordObject(m_Target, "Cancel Preset");
                m_InitialValue.ApplyTo(m_Target);
            }
           provider.Repaint();
        }
        public override void OnSelectionClosed(Preset selection)
        {
            OnSelectionChanged(selection);
            Object.DestroyImmediate(this);
        }
    }
}