using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Sygenap {
    public class Sygenap : MonoBehaviour
    {
        private int _seed;
        public int seed
        {
            get
            {
                return _seed;
            }
        }

        private bool randomSeed = true;

        public string gameName;

        private POV _pov;
        public POV pov
        {
            get { return _pov; }
        }
        public GameObject povPrefab;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if (File.Exists(this.getSaveFileURI()))
                this.loadLevel();
            else
            {
                if (this.randomSeed)
                    this._seed = Random.Range(int.MinValue, int.MaxValue);
            }
        }

        // Update is called once per frame
        void Update()
        {

        }

        public string getSaveFileURI()
        {
            return Application.persistentDataPath + "/" + this.gameName + "/." + this._seed;
        }

        public void saveLevel()
        {
            string destination = this.getSaveFileURI();
            FileStream file;

            if (File.Exists(destination)) file = File.OpenWrite(destination);
            else file = File.Create(destination);

            SygenapData data = new SygenapData(this._seed, this._pov.transform.position, this._pov.transform.localRotation);

            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(file, data);
            file.Close();
        }

        private void loadLevel()
        {
            string destination = this.getSaveFileURI();
            FileStream file;

            if (File.Exists(destination)) file = File.OpenRead(destination);
            else
            {
                Debug.LogError("File not found");
                return;
            }

            BinaryFormatter bf = new BinaryFormatter();
            SygenapData data = (SygenapData)bf.Deserialize(file);
            file.Close();

            this.spawnPOV(data.povPosition, data.povRotation);
        }

        private void spawnPOV(Vector3 position, Quaternion rotation)
        {
            GameObject povObj = Instantiate(this.povPrefab);
            povObj.transform.position = position;
            povObj.transform.localRotation = rotation;

            POV pov = povObj.AddComponent<POV>();
        }
    }

    [System.Serializable]
    public class SygenapData{
        public int seed;
        public Vector3 povPosition;
        public Quaternion povRotation;

        public SygenapData(int seed, Vector3 povPosition, Quaternion povRotation)
        {
            this.seed = seed;
            this.povPosition = povPosition;
            this.povRotation = povRotation;
        }
    }
}