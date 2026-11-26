using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
    [Header("Informacion del objeto")]
    [SerializeField] private string objectName = "Objeto interactivo";

    [TextArea(2, 4)]
    [SerializeField] private string interactionMessage = "Has interactuado con este objeto.";

    [Header("Indicador en pantalla")]
    [SerializeField] private string interactionPrompt = "[E] INTERACTUAR";

    [Header("Resaltado al mirar")]
    [SerializeField] private bool enableHighlight = true;
    [SerializeField] private Color highlightColor = new Color(0.18f, 0.85f, 1f, 1f);
    [Range(0f, 1f)]
    [SerializeField] private float highlightTintStrength = 0.48f;
    [SerializeField] private bool usePointLight = true;
    [SerializeField] private float highlightLightIntensity = 1.2f;
    [SerializeField] private float highlightLightRange = 2.2f;

    [Header("Sonido de interaccion")]
    [SerializeField] private AudioClip interactionSound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    private sealed class MaterialSlotState
    {
        public Renderer renderer;
        public int materialIndex;
        public string colorProperty;
        public Color originalColor;
        public MaterialPropertyBlock originalPropertyBlock;
    }

    private readonly List<MaterialSlotState> materialStates = new List<MaterialSlotState>();
    private MaterialPropertyBlock workingPropertyBlock;
    private Light focusLight;
    private bool isFocused;

    /// <summary>Permite que objetos usados sigan mostrando estado, o se retiren del foco.</summary>
    public virtual bool CanBeFocused => true;

    /// <summary>Indica si la interacción se puede ejecutar en este momento.</summary>
    public virtual bool CanInteract => true;

    /// <summary>Texto que verá el jugador al apuntar al objeto.</summary>
    public virtual string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    protected virtual void Awake()
    {
        workingPropertyBlock = new MaterialPropertyBlock();
        CacheRendererProperties();
        CreateFocusLight();
    }

    protected virtual void OnDisable()
    {
        SetFocused(false);
    }

    protected virtual void OnDestroy()
    {
        RestoreMaterialProperties();
    }

    private void CacheRendererProperties()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null)
                continue;

            Material[] sharedMaterials = targetRenderer.sharedMaterials;

            for (int i = 0; i < sharedMaterials.Length; i++)
            {
                Material material = sharedMaterials[i];
                if (material == null)
                    continue;

                string colorProperty = null;
                Color originalColor = Color.white;

                // URP Lit utiliza _BaseColor; Standard normalmente utiliza _Color.
                if (material.HasProperty("_BaseColor"))
                {
                    colorProperty = "_BaseColor";
                    originalColor = material.GetColor(colorProperty);
                }
                else if (material.HasProperty("_Color"))
                {
                    colorProperty = "_Color";
                    originalColor = material.GetColor(colorProperty);
                }

                if (colorProperty == null)
                    continue;

                MaterialPropertyBlock originalBlock = new MaterialPropertyBlock();
                targetRenderer.GetPropertyBlock(originalBlock, i);

                materialStates.Add(new MaterialSlotState
                {
                    renderer = targetRenderer,
                    materialIndex = i,
                    colorProperty = colorProperty,
                    originalColor = originalColor,
                    originalPropertyBlock = originalBlock
                });
            }
        }
    }

    private void CreateFocusLight()
    {
        if (!usePointLight)
            return;

        GameObject lightObject = new GameObject("Runtime_InteractionGlow");
        lightObject.transform.SetParent(transform, true);
        lightObject.transform.position = CalculateVisualCenter();

        focusLight = lightObject.AddComponent<Light>();
        focusLight.type = LightType.Point;
        focusLight.color = highlightColor;
        focusLight.intensity = highlightLightIntensity;
        focusLight.range = highlightLightRange;
        focusLight.shadows = LightShadows.None;
        focusLight.renderMode = LightRenderMode.ForcePixel;
        focusLight.enabled = false;
    }

    private Vector3 CalculateVisualCenter()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds.center;
        }

        Collider targetCollider = GetComponent<Collider>();
        return targetCollider != null ? targetCollider.bounds.center : transform.position;
    }

    /// <summary>Activa o desactiva el resaltado cuando el jugador apunta al objeto.</summary>
    public void SetFocused(bool focused)
    {
        if (isFocused == focused)
            return;

        isFocused = focused;

        if (!enableHighlight)
            return;

        if (focusLight != null)
            focusLight.enabled = focused;

        if (focused)
            ApplyMaterialHighlight();
        else
            RestoreMaterialProperties();
    }

    private void ApplyMaterialHighlight()
    {
        foreach (MaterialSlotState state in materialStates)
        {
            if (state.renderer == null)
                continue;

            state.renderer.GetPropertyBlock(workingPropertyBlock, state.materialIndex);

            Color highlighted = Color.Lerp(
                state.originalColor,
                highlightColor,
                highlightTintStrength
            );

            // Conserva la transparencia original del material.
            highlighted.a = state.originalColor.a;
            workingPropertyBlock.SetColor(state.colorProperty, highlighted);
            state.renderer.SetPropertyBlock(workingPropertyBlock, state.materialIndex);
        }
    }

    private void RestoreMaterialProperties()
    {
        foreach (MaterialSlotState state in materialStates)
        {
            if (state.renderer != null)
                state.renderer.SetPropertyBlock(state.originalPropertyBlock, state.materialIndex);
        }
    }

    public virtual void Interact()
    {
        Debug.Log(objectName + ": " + interactionMessage, gameObject);

        if (interactionSound != null)
        {
            AudioSource.PlayClipAtPoint(
                interactionSound,
                transform.position,
                soundVolume
            );
        }
    }
}
