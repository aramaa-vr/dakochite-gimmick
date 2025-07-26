using UnityEngine;
using System.Collections.Generic;

namespace Aramaa.DakochiteGimmick.Editor
{
    public class GameObjectListHolder : ScriptableObject
    {
        public List<GameObject> ignoreGameObjects = new List<GameObject>();
    }
}
