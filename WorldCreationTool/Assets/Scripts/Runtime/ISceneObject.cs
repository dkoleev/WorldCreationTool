using UnityEngine;

namespace Runtime {
    public interface ISceneObject {
        string Id { get; set; }
        Vector2Int Size { get; set; }
    }
}
