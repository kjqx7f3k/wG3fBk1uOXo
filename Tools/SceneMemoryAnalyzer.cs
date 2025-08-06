using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class ImprovedSceneMemoryAnalyzer : MonoBehaviour
{
    [System.Serializable]
    public class MemoryInfo
    {
        public string category;
        public long bytes;
        public string formattedSize;
        
        public MemoryInfo(string cat, long size)
        {
            category = cat;
            bytes = size;
            formattedSize = FormatBytes(size);
        }
    }
    
    [System.Serializable]
    public class DetailedResourceInfo
    {
        public long textureMemory;
        public long meshMemory;
        public long audioMemory;
        public long materialMemory;
        public long animationMemory;
        public int textureCount;
        public int meshCount;
        public int audioCount;
        public int materialCount;
    }
    
    public List<MemoryInfo> GetSystemMemoryUsage()
    {
        List<MemoryInfo> memoryInfos = new List<MemoryInfo>();
        
        // 強制垃圾回收
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
        
        try
        {
            long totalReserved = Profiler.GetTotalReservedMemoryLong();
            long totalAllocated = Profiler.GetTotalAllocatedMemoryLong();
            long totalUnused = Profiler.GetTotalUnusedReservedMemoryLong();
            long graphicsMemory = Profiler.GetAllocatedMemoryForGraphicsDriver();
            long managedMemory = System.GC.GetTotalMemory(false);
            
            memoryInfos.Add(new MemoryInfo("Total Reserved Memory", totalReserved));
            memoryInfos.Add(new MemoryInfo("Total Allocated Memory", totalAllocated));
            memoryInfos.Add(new MemoryInfo("Total Unused Reserved", totalUnused));
            memoryInfos.Add(new MemoryInfo("Graphics Driver Memory", graphicsMemory));
            memoryInfos.Add(new MemoryInfo("Managed Memory (GC)", managedMemory));
            
            // 計算實際使用的記憶體
            long actualUsed = totalAllocated - totalUnused;
            memoryInfos.Add(new MemoryInfo("Actual Used Memory", actualUsed));
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error getting system memory: {ex.Message}");
        }
        
        return memoryInfos;
    }
    
    public DetailedResourceInfo AnalyzeSceneResources()
    {
        DetailedResourceInfo info = new DetailedResourceInfo();
        Scene currentScene = SceneManager.GetActiveScene();
        
        if (!currentScene.IsValid())
        {
            Debug.LogError("Invalid scene");
            return info;
        }
        
        // 分析場景中的所有資源
        GameObject[] allObjects = currentScene.GetRootGameObjects();
        HashSet<Object> analyzedResources = new HashSet<Object>();
        
        foreach (GameObject rootObj in allObjects)
        {
            AnalyzeGameObjectResources(rootObj, info, analyzedResources);
        }
        
        return info;
    }
    
    private void AnalyzeGameObjectResources(GameObject obj, DetailedResourceInfo info, HashSet<Object> analyzed)
    {
        // 分析 Renderer 組件
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            // 分析 Mesh
            if (renderer is MeshRenderer meshRenderer)
            {
                MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null && !analyzed.Contains(meshFilter.sharedMesh))
                {
                    info.meshMemory += Profiler.GetRuntimeMemorySizeLong(meshFilter.sharedMesh);
                    info.meshCount++;
                    analyzed.Add(meshFilter.sharedMesh);
                }
            }
            
            // 分析 Materials 和 Textures
            if (renderer.sharedMaterials != null)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat != null && !analyzed.Contains(mat))
                    {
                        info.materialMemory += Profiler.GetRuntimeMemorySizeLong(mat);
                        info.materialCount++;
                        analyzed.Add(mat);
                        
                        // 分析材質中的貼圖
                        Shader shader = mat.shader;
                        if (shader != null)
                        {
                            for (int i = 0; i < shader.GetPropertyCount(); i++)
                            {
                                if (shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
                                {
                                    string propertyName = shader.GetPropertyName(i);
                                    Texture texture = mat.GetTexture(propertyName);
                                    
                                    if (texture != null && !analyzed.Contains(texture))
                                    {
                                        info.textureMemory += Profiler.GetRuntimeMemorySizeLong(texture);
                                        info.textureCount++;
                                        analyzed.Add(texture);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // 分析 AudioSource 組件
        AudioSource[] audioSources = obj.GetComponentsInChildren<AudioSource>(true);
        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource.clip != null && !analyzed.Contains(audioSource.clip))
            {
                info.audioMemory += Profiler.GetRuntimeMemorySizeLong(audioSource.clip);
                info.audioCount++;
                analyzed.Add(audioSource.clip);
            }
        }
        
        // 分析 Animator 組件
        Animator[] animators = obj.GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            if (animator.runtimeAnimatorController != null && !analyzed.Contains(animator.runtimeAnimatorController))
            {
                info.animationMemory += Profiler.GetRuntimeMemorySizeLong(animator.runtimeAnimatorController);
                analyzed.Add(animator.runtimeAnimatorController);
            }
        }
    }
    
    public static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024f:F2} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024f * 1024f):F2} MB";
        return $"{bytes / (1024f * 1024f * 1024f):F2} GB";
    }
    
    [ContextMenu("Full Memory Analysis")]
    public void PerformFullAnalysis()
    {
        Debug.Log("=== Full Memory Analysis Started ===");
        
        // 系統記憶體
        var systemMemory = GetSystemMemoryUsage();
        Debug.Log("=== System Memory ===");
        foreach (var info in systemMemory)
        {
            Debug.Log($"{info.category}: {info.formattedSize}");
        }
        
        // 場景資源分析
        var resourceInfo = AnalyzeSceneResources();
        Debug.Log("\n=== Scene Resources ===");
        Debug.Log($"Textures: {FormatBytes(resourceInfo.textureMemory)} ({resourceInfo.textureCount} items)");
        Debug.Log($"Meshes: {FormatBytes(resourceInfo.meshMemory)} ({resourceInfo.meshCount} items)");
        Debug.Log($"Audio: {FormatBytes(resourceInfo.audioMemory)} ({resourceInfo.audioCount} items)");
        Debug.Log($"Materials: {FormatBytes(resourceInfo.materialMemory)} ({resourceInfo.materialCount} items)");
        Debug.Log($"Animations: {FormatBytes(resourceInfo.animationMemory)}");
        
        long totalSceneResources = resourceInfo.textureMemory + resourceInfo.meshMemory + 
                                  resourceInfo.audioMemory + resourceInfo.materialMemory + 
                                  resourceInfo.animationMemory;
        Debug.Log($"Total Scene Resources: {FormatBytes(totalSceneResources)}");
        
        Debug.Log("=== Analysis Complete ===");
    }
}