using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DeepLearningImageProcessor
{
    /// <summary>
    /// The ImageProcessor class processes images using compute shaders or materials,
    /// normalizing and resizing them according to the specified parameters.
    /// </summary>
    public class ImageProcessor : MonoBehaviour
    {
        [Header("Processing Shaders")]
        [Tooltip("The compute shader for image processing")]
        [SerializeField] private ComputeShader processingComputeShader;
        [Tooltip("The material for image normalization")]
        [SerializeField] private Material normalizeMaterial;
        [Tooltip("The material for image cropping")]
        [SerializeField] private Material cropMaterial;

        [Header("Normalization Parameters")]
        [Tooltip("JSON file with the mean and std values for normalization")]
        [SerializeField] private TextAsset normStatsJson = null;

        // If you want to set default assets from your project, use the GUID of the asset
        // You can find the GUID in the .meta file of the asset (open it with a text editor)
        private const string ProcessingComputeShaderGUID = "2c418cec15ae44419d94328d0e8dcea8";
        private const string NormalizeMaterialGUID = "98c21923ad2f420496e34b649c6cad3e";
        private const string CropMaterialGUID = "d6db137a4c2b476cbe1ddc99af13beb7";
        private const string NormStatsJsonGUID = "9c8f1a57cb884c9b8a4439cae327a2f8";

        [System.Serializable]
        private class NormStats
        {
            public float[] mean;
            public float[] std;
            public float scale;
        }

        // The mean values for normalization
        private float[] mean = new float[] { 0f, 0f, 0f };
        // The standard deviation values for normalization
        private float[] std = new float[] { 1f, 1f, 1f };
        // Value used to scale normalized input
        private float scale = 1f;

        // Buffer for mean values used in compute shader
        private ComputeBuffer meanBuffer;
        // Buffer for standard deviation values used in compute shader
        private ComputeBuffer stdBuffer;


        private void Reset()
        {
            // Use the AssetDatabase to find the asset by its GUID and set the default values
            // This will only work in the Unity Editor, not in a build
#if UNITY_EDITOR
        if (processingComputeShader == null)
        {
            processingComputeShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(UnityEditor.AssetDatabase.GUIDToAssetPath(ProcessingComputeShaderGUID));
        }

        if (normalizeMaterial == null)
        {
            normalizeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(UnityEditor.AssetDatabase.GUIDToAssetPath(NormalizeMaterialGUID));
        }

        if (cropMaterial == null)
        {
            cropMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(UnityEditor.AssetDatabase.GUIDToAssetPath(CropMaterialGUID));
        }

        if (normStatsJson == null)
        {
            normStatsJson = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(UnityEditor.AssetDatabase.GUIDToAssetPath(NormStatsJsonGUID));
        }
#endif
        }

        /// <summary>
        /// Called when the script is initialized.
        /// </summary>
        private void Start()
        {
            LoadNormStats();
            InitializeProcessingShaders();
        }

        /// <summary>
        /// Load the normalization stats from the provided JSON file.
        /// </summary>
        private void LoadNormStats()
        {
            if (IsNormStatsJsonNullOrEmpty())
            {
                return;
            }

            NormStats normStats = DeserializeNormStats(normStatsJson.text);
            UpdateNormalizationStats(normStats);
        }

        /// <summary>
        /// Check if the provided JSON file is null or empty.
        /// </summary>
        /// <returns>True if the file is null or empty, otherwise false.</returns>
        private bool IsNormStatsJsonNullOrEmpty()
        {
            return normStatsJson == null || string.IsNullOrWhiteSpace(normStatsJson.text);
        }

        /// <summary>
        /// Deserialize the provided JSON string to a NormStats object.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A deserialized NormStats object.</returns>
        private NormStats DeserializeNormStats(string json)
        {
            try
            {
                return JsonUtility.FromJson<NormStats>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to deserialize normalization stats JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Update the mean and standard deviation with the provided NormStats object.
        /// </summary>
        /// <param name="normStats">The NormStats object containing the mean and standard deviation.</param>
        private void UpdateNormalizationStats(NormStats normStats)
        {
            if (normStats == null)
            {
                return;
            }

            mean = normStats.mean;
            std = normStats.std;
            scale = normStats.scale == 0f ? 1f : normStats.scale;
        }


        /// <summary>
        /// Initializes the processing shaders by setting the mean and standard deviation values.
        /// </summary>
        private void InitializeProcessingShaders()
        {
            normalizeMaterial.SetFloatArray("_Mean", mean);
            normalizeMaterial.SetFloatArray("_Std", std);
            normalizeMaterial.SetFloat("_Scale", scale);

            if (SystemInfo.supportsComputeShaders)
            {
                int kernelIndex = processingComputeShader.FindKernel("NormalizeImage");

                meanBuffer = CreateComputeBuffer(mean);
                stdBuffer = CreateComputeBuffer(std);

                processingComputeShader.SetBuffer(kernelIndex, "_Mean", meanBuffer);
                processingComputeShader.SetBuffer(kernelIndex, "_Std", stdBuffer);
                processingComputeShader.SetFloat("_Scale", scale);
            }
        }

        /// <summary>
        /// Creates a compute buffer and sets the provided data.
        /// </summary>
        /// <param name="data">The data to set in the compute buffer.</param>
        /// <returns>A compute buffer with the provided data.</returns>
        private ComputeBuffer CreateComputeBuffer(float[] data)
        {
            ComputeBuffer buffer = new ComputeBuffer(data.Length, sizeof(float));
            buffer.SetData(data);
            return buffer;
        }

        /// <summary>
        /// Processes an image using a compute shader with the specified function name.
        /// </summary>
        /// <param name="image">The image to be processed.</param>
        /// <param name="functionName">The name of the function in the compute shader to use for processing.</param>
        public void ProcessImageComputeShader(RenderTexture image, string functionName)
        {
            int kernelHandle = processingComputeShader.FindKernel(functionName);
            RenderTexture result = GetTemporaryRenderTexture(image);

            BindTextures(kernelHandle, image, result);
            DispatchShader(kernelHandle, result);
            Graphics.Blit(result, image);

            RenderTexture.ReleaseTemporary(result);
        }

        /// <summary>
        /// Processes an image using a material.
        /// </summary>
        /// <param name="image">The image to be processed.</param>
        public void ProcessImageShader(RenderTexture image)
        {
            RenderTexture result = GetTemporaryRenderTexture(image, false);

            RenderTexture.active = result;
            Graphics.Blit(image, result, normalizeMaterial);
            Graphics.Blit(result, image);

            RenderTexture.ReleaseTemporary(result);
        }

        /// <summary>
        /// Creates a temporary render texture with the same dimensions as the given image.
        /// </summary>
        /// <param name="image">The image to match dimensions with.</param>
        /// <param name="enableRandomWrite">Enable random access write into the RenderTexture.</param>
        /// <returns>A temporary render texture.</returns>
        private RenderTexture GetTemporaryRenderTexture(RenderTexture image, bool enableRandomWrite = true)
        {
            RenderTexture result = RenderTexture.GetTemporary(image.width, image.height, 24, RenderTextureFormat.ARGBHalf);
            result.enableRandomWrite = enableRandomWrite;
            result.Create();
            return result;
        }

        /// <summary>
        /// Binds the source and destination textures to the compute shader.
        /// </summary>
        /// <param name="kernelHandle">The kernel handle of the compute shader.</param>
        /// <param name="source">The source texture to be processed.</param>
        /// <param name="destination">The destination texture for the processed result.</param>
        private void BindTextures(int kernelHandle, RenderTexture source, RenderTexture destination)
        {
            processingComputeShader.SetTexture(kernelHandle, "_Result", destination);
            processingComputeShader.SetTexture(kernelHandle, "_InputImage", source);
        }

        /// <summary>
        /// Dispatches the compute shader based on the dimensions of the result texture.
        /// </summary>
        /// <param name="kernelHandle">The kernel handle of the compute shader.</param>
        /// <param name="result">The result render texture.</param>
        private void DispatchShader(int kernelHandle, RenderTexture result)
        {
            int threadGroupsX = Mathf.CeilToInt((float)result.width / 8);
            int threadGroupsY = Mathf.CeilToInt((float)result.height / 8);
            processingComputeShader.Dispatch(kernelHandle, threadGroupsX, threadGroupsY, 1);
        }

        /// <summary>
        /// Calculates the input dimensions of the processed image based on the original image dimensions.
        /// </summary>
        /// <param name="imageDims">The dimensions of the original image.</param>
        /// <returns>The calculated input dimensions for the processed image.</returns>
        public Vector2Int CalculateInputDims(Vector2Int imageDims, int targetDim = 224)
        {
            targetDim = Mathf.Max(targetDim, 64);
            float scaleFactor = (float)targetDim / Mathf.Min(imageDims.x, imageDims.y);
            return Vector2Int.RoundToInt(new Vector2(imageDims.x * scaleFactor, imageDims.y * scaleFactor));
        }


        public void CropImageComputeShader(RenderTexture image, RenderTexture croppedImage, Vector2Int offset, Vector2Int size)
        {
            int kernelHandle = processingComputeShader.FindKernel("Crop");
            RenderTexture result = GetTemporaryRenderTexture(croppedImage);

            BindTextures(kernelHandle, image, result);
            processingComputeShader.SetInts("_Offset", new int[] { offset.x, offset.y });
            processingComputeShader.SetInts("_Size", new int[] { size.x, size.y });
            DispatchShader(kernelHandle, result);
            Graphics.Blit(result, croppedImage);

            RenderTexture.ReleaseTemporary(result);
        }

        public void CropImageShader(RenderTexture image, RenderTexture croppedImage, float[] offset, float[] size)
        {
            cropMaterial.SetVector("_Offset", new Vector4(offset[0], offset[1], 0, 0));
            cropMaterial.SetVector("_Size", new Vector4(size[0], size[1], 0, 0));

            RenderTexture result = GetTemporaryRenderTexture(croppedImage, false);

            RenderTexture.active = result;
            Graphics.Blit(image, result, cropMaterial);
            Graphics.Blit(result, croppedImage);

            RenderTexture.ReleaseTemporary(result);
        }


        /// <summary>
        /// Called when the script is disabled.
        /// </summary>
        private void OnDisable()
        {
            ReleaseComputeBuffers();
        }

        /// <summary>
        /// Releases the compute buffers if compute shaders are supported.
        /// </summary>
        private void ReleaseComputeBuffers()
        {
            if (SystemInfo.supportsComputeShaders)
            {
                meanBuffer?.Release();
                stdBuffer?.Release();
            }
        }
    }

}

