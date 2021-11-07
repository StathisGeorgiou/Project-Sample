using ARPolis.Data;
using StaGeUnityTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARPolis.UI
{

    public class PoiItem : MonoBehaviour
    {

        public string poiID;
        public PoiEntity poiEntity;
        RawImage img;

        public void Init()
        {
            Button btn = GetComponent<Button>();
            img = GetComponent<RawImage>();

            if (btn)
            {
                btn.onClick.AddListener(() => GlobalActionsUI.OnPoiSelected?.Invoke(poiID));
            }
        }

        public void SetImage(Texture2D spr) { if (img) img.texture = spr; }

    }

}
