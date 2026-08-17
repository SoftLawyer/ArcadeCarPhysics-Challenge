using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingLights : MonoBehaviour {
    public int windowMaterialIndex;
    public Color lightColor;
    public bool areLightsOn;
    private Color defaultColor;
    private MeshRenderer mr;

    private void Start() {
        mr = GetComponent<MeshRenderer>();
        if (mr != null && mr.materials != null && windowMaterialIndex < mr.materials.Length) {
            defaultColor = mr.materials[windowMaterialIndex].color;
            SetLights(areLightsOn);
        }
    }

    public void SetLights(bool isOn) {
        if (mr != null && mr.materials != null && windowMaterialIndex < mr.materials.Length) {
            // Şaderi DEĞİŞTİRME, sadece rengini değiştir. Aksi takdirde CurvedToonCity bükülme efekti kırılır!
            mr.materials[windowMaterialIndex].color = isOn ? lightColor : defaultColor;
        }
    }
}
