using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CJM.DeepLearningImageProcessor
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
        [Tooltip("The shader for image normalization")]
        [SerializeField] private Shader normalizeShader;
        [Tooltip("The shader for image cropping")]
        [SerializeField] private Shader cropShader;

        [Header("Normalization Parameters")]
        [Tooltip("JSON file with the mean and std values for normalization")]
        [SerializeField] private TextAsset normStatsJson = null;

        // GUIDs of the default assets used for shaders and normalization
        private const string ProcessingComputeShaderGUID = "2c418cec15ae44419d94328d0e8dcea8";
        private const string NormalizeShaderGUID = "45d8405a4cc64ecfa477b712e0465c05";
        private const string CropShaderGUID = "0685d34a035b4cefa942d94390282c12";
        private const string NormStatsJsonGUID = "9c8f1a57cb884c9b8a4439cae327a2f8";

        // The material for image normalization
        private Material normalizeMaterial;
        // The material for image cropping
        private Material cropMaterial;

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

        /// <summary>
        /// Reset is called when the user hits the Reset button in the Inspector's context menu
        /// or when adding the component the first time. This function is only called in editor mode.
        /// </summary>
        private void Reset()
        {
            // Load default assets only in the Unity Editor, not in a build
#if UNITY_EDITOR
            processingComputeShader = LoadDefaultAsset<ComputeShader>(ref processingComputeShader, ProcessingComputeShaderGUID);
            normalizeShader = LoadDefaultAsset<Shader>(ref normalizeShader, NormalizeShaderGUID);
            cropShader = LoadDefaultAsset<Shader>(ref cropShader, CropShaderGUID);
            normStatsJson = LoadDefaultAsset<TextAsset>(ref normStatsJson, NormStatsJsonGUID);
#endif
        }


        /// <summary>
        /// Loads the default asset for the specified field using its GUID.
        /// </summary>
        /// <typeparam name="T">The type of asset to be loaded.</typeparam>
        /// <param name="asset">A reference to the asset field to be assigned.</param>
        /// <param name="guid">The GUID of the default asset.</param>
        /// <remarks>
        /// This method is only executed in the Unity Editor, not in builds.
        /// </remarks>
        private void LoadDefaultAsset<T>(ref T asset, string guid) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            // Check if the asset field is null
            if (asset == null)
            {
                // Load the asset from the AssetDatabase using its GUID
                asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            }
#endif
        }



        /// <summary>
        /// Called when the script is initialized.
        /// </summary>
        private void Start()
        {
            normalizeMaterial = new Material(normalizeShader);
            cropMaterial = new Material(cropShader);

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
            // Disable scaling if no scale value is provided
            scale = normStats.scale == 0f ? 1f : normStats.scale;
        }


        /// <summary>
        /// Initializes the processing shaders by setting the mean and standard deviation values.
        /// </summary>
        private void InitializeProcessingShaders()
        {
            normalizeMaterial.SetVector("_Mean", new Vector4(mean[0], mean[1], mean[2], 0));
            normalizeMaterial.SetVector("_Std", new Vector4(std[0], std[1], std[2], 0));
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
            // Create a temporary render texture
            RenderTexture result = GetTemporaryRenderTexture(image);

            // Bind the source and destination textures to the compute shader
            BindTextures(kernelHandle, image, result);
            // Dispatche the shader
            DispatchShader(kernelHandle, result);
            // Blit the processed image back to the original image
            Graphics.Blit(result, image);

            RenderTexture.ReleaseTemporary(result);
        }

        /// <summary>
        /// Processes an image using a material.
        /// </summary>
        /// <param name="image">The image to be processed.</param>
        public void ProcessImageShader(RenderTexture image)
        {
            // Create a temporary render texture
            RenderTexture result = GetTemporaryRenderTexture(image, false);
            RenderTexture.active = result;
            // Apply the normalization material to the input image
            Graphics.Blit(image, result, normalizeMaterial);
            // Copy the result back to the original image
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
            // Create a temporary render texture
            RenderTexture result = RenderTexture.GetTemporary(image.width, image.height, 24, RenderTextureFormat.ARGBHalf);
            // Set random write access
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
            processingComputeShader.SetTexture(kernelHandle, "_OutputImage", destination);
            processingComputeShader.SetTexture(kernelHandle, "_InputImage", source);
        }

        /// <summary>
        /// Dispatches the compute shader based on the dimensions of the result texture.
        /// </summary>
        /// <param name="kernelHandle">The kernel handle of the compute shader.</param>
        /// <param name="result">The result render texture.</param>
        private void DispatchShader(int kernelHandle, RenderTexture result)
        {
            // Calculate the thread groups in the X and Y dimensions
            int threadGroupsX = Mathf.CeilToInt((float)result.width / 8);
            int threadGroupsY = Mathf.CeilToInt((float)result.height / 8);
            // Execute the compute shader
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


        /// <summary>
        /// Crops an image using a compute shader with the given offset and size.
        /// </summary>
        /// <param name="image">The original image to be cropped.</param>
        /// <param name="croppedImage">The cropped output image.</param>
        /// <param name="offset">The offset for the crop area in the original image.</param>
        /// <param name="size">The size of the crop area.</param>
        public void CropImageComputeShader(RenderTexture image, RenderTexture croppedImage, Vector2Int offset, Vector2Int size)
        {
            int kernelHandle = processingComputeShader.FindKernel("CropImage");
            RenderTexture result = GetTemporaryRenderTexture(croppedImage);

            // Bind the source and destination textures to the compute shader
            BindTextures(kernelHandle, image, result);
            // Set the offset and size parameters
            processingComputeShader.SetInts("_CropOffset", new int[] { offset.x, offset.y });
            processingComputeShader.SetInts("_CropSize", new int[] { size.x, size.y });
            // Execute the compute shader
            DispatchShader(kernelHandle, result);
            // Copy the result to the cropped image texture
            Graphics.Blit(result, croppedImage);

            RenderTexture.ReleaseTemporary(result);
        }


        /// <summary>
        /// Crops an image using a shader with the given offset and size.
        /// </summary>
        /// <param name="image">The original image to be cropped.</param>
        /// <param name="croppedImage">The cropped output image.</param>
        /// <param name="offset">The offset for the crop area in the original image (float array with two elements).</param>
        /// <param name="size">The size of the crop area (float array with two elements).</param>

        public void CropImageShader(RenderTexture image, RenderTexture croppedImage, float[] offset, float[] size)
        {
            // Set the offset and size parameters on the crop material
            cropMaterial.SetVector("_Offset", new Vector4(offset[0], offset[1], 0, 0));
            cropMaterial.SetVector("_Size", new Vector4(size[0], size[1], 0, 0));

            // Create a temporary render texture
            RenderTexture result = GetTemporaryRenderTexture(croppedImage, false);
            RenderTexture.active = result;

            // Apply the crop material to the input image
            Graphics.Blit(image, result, cropMaterial);
            // Copy the result to the cropped image texture
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

