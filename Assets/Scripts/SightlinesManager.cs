using System.Collections.Generic;
using UnityEngine;

public class SightlinesManager : MonoBehaviour
{
    [System.Serializable]
    public struct SightlineDefines
    {
        public Bounds bounds;
        public List<string> activations;
    }

    // ...need to be sepearated and not in one dict for some reason or else they don't get stored between recompiles???
    public List<string> sightlineKeys = new List<string>();
    public List<SightlineDefines> sightlineValues = new List<SightlineDefines>();
    private Dictionary<string, SightlineDefines> _sightlineMapping = new Dictionary<string, SightlineDefines>();
    public string InitializeSightlineLayer = "MyPrison";

    #region Editor Functions
    public void EDITOR_AddSightLine(string key, SightlineDefines value)
    {
        Debug.Assert(Application.isEditor);
        if (sightlineKeys.Contains(key))
        {
            sightlineValues[sightlineKeys.IndexOf(key)] = value;
        }
        else
        {
            sightlineKeys.Add(key);
            sightlineValues.Add(value);
        }
    }
    public SightlineDefines EDITOR_GetSightLine(string key)
    {
        Debug.Assert(Application.isEditor);
        if (!sightlineKeys.Contains(key))
        {
            throw new System.ArgumentOutOfRangeException(key, "Sightline " + key + " not defined");
        }
        return sightlineValues[sightlineKeys.IndexOf(key)];
    }
    #endregion

    public bool SightlinesPrebaked { get; set; } = false;
    private Dictionary<string, List<MeshRenderer>> _layerIdToMeshesMapping;
    private List<KeyValuePair<Bounds, string>> _boundsToLayer;

    private void OnEnable()
    {
        Debug.Assert(sightlineKeys.Count == sightlineValues.Count);
        if (!SightlinesPrebaked) // lol im probably not going to do prebaking but if I do.... do the shit here
        {
            //Debug.LogWarning("Sightlines are NOT prebaked!");
            for (int i = 0; i < sightlineKeys.Count; ++i)
            {
                _sightlineMapping.Add(sightlineKeys[i], sightlineValues[i]);
            }
            _layerIdToMeshesMapping = new Dictionary<string, List<MeshRenderer>>();
            foreach (string layer in sightlineKeys)
            {
                _layerIdToMeshesMapping.Add(layer, new List<MeshRenderer>());
            }
            SightlinesLayer[] allMeshes = FindObjectsOfType<SightlinesLayer>();
            foreach (var entry in allMeshes)
            {
                MeshRenderer rend = entry.GetComponent<MeshRenderer>();
                if (rend.enabled)
                {
                    foreach (string layerName in entry.layerNames)
                    {
                        Debug.Assert(_layerIdToMeshesMapping.ContainsKey(layerName), "Attempting to add meshes to layer " + layerName + " which isn't in the mapping");
                        // I'm running out of time lol
                        _layerIdToMeshesMapping[layerName].Add(rend);
                    }
                }
                if (entry.TryGetComponent(out Shapes.ShapeRenderer shapeRend))
                {
                    shapeRend.enabled = false;
                }
                rend.enabled = false;
            }

            _boundsToLayer = new List<KeyValuePair<Bounds, string>>();
            for (int i = 0; i < sightlineValues.Count; ++i)
            {
                _boundsToLayer.Add(new KeyValuePair<Bounds, string>(sightlineValues[i].bounds, sightlineKeys[i]));
            }
        }
        ActivateLayer(InitializeSightlineLayer);
    }

    protected void ActivateLayer(string layer)
    {
        List<string> activations = _sightlineMapping[layer].activations;
        //Debug.Log("Activation detected: activating chunk " + layer + ",which has " + activations.Count + " activations.");
        foreach (string activatedLayer in activations)
        {
            if (!_layerIdToMeshesMapping.ContainsKey(activatedLayer)) continue; // Already revealed
            List<MeshRenderer> filtersToEnable = _layerIdToMeshesMapping[activatedLayer];
            filtersToEnable.ForEach(x =>
            {
                x.enabled = true;
                if (x.TryGetComponent(out Shapes.ShapeRenderer shapesRend))
                {
                    shapesRend.enabled = true;
                }
            });
            _layerIdToMeshesMapping.Remove(activatedLayer);
        }
    }
    
    public bool TestForSightlines(Vector3 position)
    {
        bool sightlinesHit = false;
        // Split this up by floor
        for (int i = 0; i < _boundsToLayer.Count; ++i)
        {
            var pair = _boundsToLayer[i];
            if (pair.Key.Contains(position))
            {
                sightlinesHit = true;
                ActivateLayer(pair.Value);
            }
        }
        return sightlinesHit;
    }


    public void AddMeshRendererToLayer(string layerID, MeshRenderer filter)
    {
        if (!_layerIdToMeshesMapping.ContainsKey(layerID))
        {
            _layerIdToMeshesMapping.Add(layerID, new List<MeshRenderer>());
        }
        _layerIdToMeshesMapping[layerID].Add(filter);
    }
}