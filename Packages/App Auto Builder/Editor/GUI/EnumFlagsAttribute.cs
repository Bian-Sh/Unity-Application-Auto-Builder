﻿using System;
using UnityEditor;
using UnityEngine;

namespace zFramework.AppBuilder
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EnumFlagsAttribute : PropertyAttribute { }

    [CustomPropertyDrawer(typeof(EnumFlagsAttribute))]
    public class EnumFlagsAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
        {
            var typeAttr = attribute as EnumFlagsAttribute;
            label.text = label.text + "(" + prop.intValue + ")";
            prop.intValue = DrawBitMaskField(position, prop.intValue, fieldInfo.FieldType, label);
        }
        //https://answers.unity.com/questions/393992/custom-inspector-multi-select-enum-dropdown.html
        public static int DrawBitMaskField(Rect aPosition, int aMask, System.Type aType, GUIContent aLabel)
        {
            var itemNames = Enum.GetNames(aType);
            var itemValues = Enum.GetValues(aType) as int[];

            int val = aMask;
            int maskVal = 0;
            for (int i = 0; i < itemValues.Length; i++)
            {
                if (itemValues[i] != 0)
                {
                    if ((val & itemValues[i]) == itemValues[i])
                        maskVal |= 1 << i;
                }
                else if (val == 0)
                    maskVal |= 1 << i;
            }
            int newMaskVal = EditorGUI.MaskField(aPosition, aLabel, maskVal, itemNames);
            int changes = maskVal ^ newMaskVal;

            for (int i = 0; i < itemValues.Length; i++)
            {
                if ((changes & (1 << i)) != 0)            // has this list item changed?
                {
                    if ((newMaskVal & (1 << i)) != 0)     // has it been set?
                    {
                        if (itemValues[i] == 0)           // special case: if "0" is set, just set the val to 0
                        {
                            val = 0;
                            break;
                        }
                        else
                            val |= itemValues[i];
                    }
                    else                                  // it has been reset
                    {
                        val &= ~itemValues[i];
                    }
                }
            }
            return val;
        }
    }
}
