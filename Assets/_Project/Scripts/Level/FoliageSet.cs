using UnityEngine;

namespace BlackCommission.Level
{
    /// <summary>
    /// Data-driven foliage set the <see cref="MapSiteBuilder"/> instantiates instead of whitebox primitives
    /// (PM 2026-06-20: real plants). Stored in Resources so the RUNTIME builder can load it with no scene wiring
    /// (<c>Resources.Load&lt;FoliageSet&gt;("FoliageSet")</c>). If the asset is missing or its arrays are empty the
    /// builder falls back to the procedural primitives, so the generator stays runtime-safe with or without assets.
    /// Authored by <c>Tools ▸ Black Commission ▸ Map ▸ Setup Foliage (URP + Resources)</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Black Commission/Foliage Set")]
    public class FoliageSet : ScriptableObject
    {
        [Tooltip("Tree models/prefabs (any scale — auto-fitted to TreeHeight at placement).")]
        public GameObject[] trees;

        [Tooltip("Bush / undergrowth models/prefabs (auto-fitted to BushHeight).")]
        public GameObject[] bushes;

        [Tooltip("Target trunk height in metres; each tree is auto-scaled to this (× the per-instance scale).")]
        public float treeHeight = 6f;

        [Tooltip("Target bush/fern height in metres.")]
        public float bushHeight = 0.9f;

        [Tooltip("Trunk collider radius (the ONLY nav obstacle a tree adds — keeps the forest navmesh-passable).")]
        public float trunkRadius = 0.35f;

        [Tooltip("Material force-applied to instantiated trees (the imported FBX material binding is unreliable).")]
        public Material treeMaterial;

        [Tooltip("Material force-applied to instantiated bushes.")]
        public Material bushMaterial;

        [Tooltip("Tiled grass material for the outdoor ground (null → builder uses its dark whitebox colour).")]
        public Material groundMaterial;

        [Tooltip("Unity-Terrain base grass layer. Non-null → builder makes an undulating Terrain (LC-style: heightmap"
               + " + GPU-instanced detail grass) instead of a mesh. Null → falls back to the grass-textured mesh.")]
        public TerrainLayer grassLayer;

        [Tooltip("Grass billboard texture for the Terrain detail layer (FALLBACK only — URP often fails to render "
               + "texture/billboard terrain grass; prefer grassDetailPrefab).")]
        public Texture2D grassDetail;

        [Tooltip("Grass-card MESH prototype for the Terrain detail layer. Non-null → MapSiteBuilder uses INSTANCED "
               + "Vertex-Lit detail meshes (the 'real' 3-D grass that renders reliably in URP); grassDetail is the "
               + "fallback. Authored by Setup Foliage.")]
        public GameObject grassDetailPrefab;

        [Tooltip("URP terrain material template (Universal Render Pipeline/Terrain/Lit) so the Terrain isn't magenta.")]
        public Material terrainMaterial;

        public bool HasTrees => trees != null && trees.Length > 0;
        public bool HasBushes => bushes != null && bushes.Length > 0;
    }
}
