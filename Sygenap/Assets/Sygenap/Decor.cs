using UnityEngine;

namespace Sygenap
{
    public class Decor : MonoBehaviour
    {
        public Sygenap root;
        public Parcel parent;
        public string uri;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public DecorData serialize()
        {
            return new DecorData(this.transform.position, this.uri);
        }
    }

    [System.Serializable]
    public class DecorData
    {
        public float positionX;
        public float positionY;
        public float positionZ;

        public string uri;

        public DecorData(Vector3 position, string uriInProject)
        {
            this.positionX = position.x;
            this.positionY = position.y;
            this.positionZ = position.z;

            this.uri = uriInProject;
        }
    }
}