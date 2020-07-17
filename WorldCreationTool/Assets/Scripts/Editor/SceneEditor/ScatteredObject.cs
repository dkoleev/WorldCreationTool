using System;
using Editor.SceneEditor.DataTypes;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR

#endif

namespace Editor.SceneEditor {
    public class ScatteredObject : ICloneable {
        public bool enable;
        public bool isPrefab = false;


        public bool isRotationOption = false;
        public bool isRotationX = false;

        public bool isRotationY = false;

        public bool isRotationZ = false;

        public bool isScale = false;
        public bool isScaleLight = true;

        public bool isScaleOption = false;
        public bool isScaleParticle = true;

        public bool isWait2delete = false;

        public float offset = 0;
        public GameObject prefab;

        public Texture2D preview;
        public string SceneObjectKey;
        public bool showOption = false;
        public bool uniformScale = true;
        public float xMaxRot = 25;
        public float xmaxScale = 1.2f;
        public float xMinRot = -25;
        public float xMinScale = 0.8f;
        public float yMaxRot = 180;
        public float yMaxScale = 1.2f;
        public float yMinRot = -180;

        public float yMinScale = 0.8f;
        public float zMaxRot = 25;
        public float zMaxScale = 1.2f;
        public float zMinRot = -25;

        public float zMinScale = 0.8f;

        public object Clone() {
            return MemberwiseClone();
        }

        public Point GetSize() {
            //TODO: object size
            return new Point(1, 1);
        }

        public static void ApplyRS(Transform tr, ScatteredObject scatter) {
            // Scale
            if (scatter.isScale) {
                if (scatter.uniformScale) {
                    var scale = Random.Range(scatter.xMinScale, scatter.xmaxScale);
                    tr.localScale = new Vector3(scale, scale, scale);
                    ScaleEffect(tr, scale, scatter.isScaleLight, scatter.isScaleParticle);
                } else {
                    tr.localScale = new Vector3(Random.Range(scatter.xMinScale, scatter.xmaxScale),
                        Random.Range(scatter.yMinScale, scatter.yMaxScale),
                        Random.Range(scatter.zMinScale, scatter.zMaxScale));
                    ScaleEffect(tr, (tr.localScale.x + tr.localScale.y + tr.localScale.z) / 3f, scatter.isScaleLight,
                        scatter.isScaleParticle);
                }
            }

            // Rotation
            if (scatter.isRotationX || scatter.isRotationY || scatter.isRotationZ) {
                var rotation = tr.eulerAngles;
                if (scatter.isRotationX) {
                    rotation.x = Random.Range(scatter.xMinRot, scatter.xMaxRot);
                }

                if (scatter.isRotationY) {
                    rotation.y = Random.Range(scatter.yMinRot, scatter.yMaxRot);
                }

                if (scatter.isRotationZ) {
                    rotation.z = Random.Range(scatter.zMinRot, scatter.zMaxRot);
                }

                tr.eulerAngles = rotation;
            }
        }

        public static void ScaleEffect(Transform tr, float scaleFactor, bool isScaleLight, bool isScaleParticle) {
#if UNITY_EDITOR
            // Light
            if (isScaleLight) {
                var lights = tr.GetComponentsInChildren<Light>();
                foreach (var light in lights) {
                    light.range *= scaleFactor;
                }
            }

            if (isScaleParticle) {
                var systems = tr.GetComponentsInChildren<ParticleSystem>();
                foreach (var system in systems) {
                    system.startSpeed *= scaleFactor;
                    system.startSize *= scaleFactor;
                    system.gravityModifier *= scaleFactor;

                    var so = new SerializedObject(system);
                    so.FindProperty("VelocityModule.x.scalar").floatValue *= scaleFactor;
                    so.FindProperty("VelocityModule.y.scalar").floatValue *= scaleFactor;
                    so.FindProperty("VelocityModule.z.scalar").floatValue *= scaleFactor;
                    so.FindProperty("ClampVelocityModule.magnitude.scalar").floatValue *= scaleFactor;
                    so.FindProperty("ClampVelocityModule.x.scalar").floatValue *= scaleFactor;
                    so.FindProperty("ClampVelocityModule.y.scalar").floatValue *= scaleFactor;
                    so.FindProperty("ClampVelocityModule.z.scalar").floatValue *= scaleFactor;
                    so.FindProperty("ForceModule.x.scalar").floatValue *= scaleFactor;
                    so.FindProperty("ForceModule.y.scalar").floatValue *= scaleFactor;
                    so.FindProperty("ForceModule.z.scalar").floatValue *= scaleFactor;
                    so.FindProperty("ColorBySpeedModule.range").vector2Value *= scaleFactor;
                    so.FindProperty("SizeBySpeedModule.range").vector2Value *= scaleFactor;
                    so.FindProperty("RotationBySpeedModule.range").vector2Value *= scaleFactor;

                    so.ApplyModifiedProperties();
                }
            }
#endif
        }
    }
}
