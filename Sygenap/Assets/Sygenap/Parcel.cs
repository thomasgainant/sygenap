using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Sygenap
{
    public class Parcel : MonoBehaviour
    {
        public Sygenap root;

        private int _x;
        public int x { get { return _x; } }

        private int _y;
        public int y { get { return _y; } }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        public void init(Sygenap root, int x, int y)
        {
            this.root = root;
            this._x = x;
            this._y = y;
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void save()
        {
            string destination = Application.persistentDataPath + "/" + this.root.gameName + "/" + this._x + "-" + this._y;
            FileStream file;

            if (File.Exists(destination)) file = File.OpenWrite(destination);
            else file = File.Create(destination);

            ParcelData data = new ParcelData(this._x, this._y);

            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(file, data);
            file.Close();
        }

        public void load()
        {
            string destination = Application.persistentDataPath + "/" + this.root.gameName + "/" + this._x + "-" + this._y;
            FileStream file;

            if (File.Exists(destination)) file = File.OpenRead(destination);
            else
            {
                Debug.LogError("File not found");
                return;
            }

            BinaryFormatter bf = new BinaryFormatter();
            ParcelData data = (ParcelData)bf.Deserialize(file);
            file.Close();
        }
    }

    [System.Serializable]
    public class ParcelData
    {
        public int x;
        public int y;

        public ParcelData(int x, int y) {
            this.x = x;
            this.y = y;
        }
    }
}