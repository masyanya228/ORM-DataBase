using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
namespace ORMDataBase
{
    public class DataBase
    {
        public List<string> blocks = new List<string>();
        public string LastName;
        public string Folder = Application.ExecutablePath;
        public string Name;
        public int ObjectsPerFile = 100;
        public string FirstId = "100";
        public DataBase(string name)
        {
            Folder = Application.ExecutablePath;
            Folder = Folder.Remove(Folder.LastIndexOf('\\'));
            Folder += @"\Tech\КАТТ\Students";
            Name = name;
            Init();
        }
        public DataBase(string name, string Folder)
        {
            this.Folder = Folder + "\\" + name;
            Name = name;
            Init();
        }
        void Init()
        {
            ReadBlocksName();
            var res = ReadBlock<IORMBase>(GetLastBlockName());
            if (res.Count > 0)
            {
                LastName = res.Last().Id ?? FirstId;
                LastName = xtensions.GetNext(LastName);
            }
            else
                LastName = FirstId;
        }

        /// <summary>
        /// Return last block ID
        /// </summary>
        /// <returns></returns>
        string GetLastBlockName()
        {
            int maxid = 0;
            string[] paths = blocks.ToArray();
            if (paths.Length > 0)
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    if (int.TryParse(paths[i], out int firstId))
                    {
                        if (maxid < firstId)
                            maxid = firstId;
                    }
                }
                return maxid + "";
            }
            return FirstId;
        }

        /// <summary>
        /// Return all block`s IDs.
        /// </summary>
        /// <returns></returns>
        object ReadBlocksName()
        {
            if (!Directory.Exists(Folder)) Directory.CreateDirectory(Folder);
            blocks.Clear();
            string[] paths = Directory.GetFiles(Folder, "*.dicdb");
            for (int i = 0; i < paths.Length; i++)
            {
                FileInfo fileInfo = new FileInfo(paths[i]);
                string Name = fileInfo.Name.Remove(fileInfo.Name.Length - fileInfo.Extension.Length);
                if (blocks.IndexOf(Name) == -1) blocks.Add(Name);
            }
            return blocks;
        }

        /// <summary>
        /// Converting Object to String
        /// </summary>
        /// <param name="iORMBase"></param>
        /// <returns></returns>
        public string ObjectToString(IORMBase iORMBase)
        {
            Type type = iORMBase.GetType();
            string all = iORMBase.Id;
            var property = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (int j = 0; j < property.Length; j++)
            {
                all += "\t" + property[j].Name + "=" + property[j].GetValue(iORMBase);
            }
            return all;
        }

        /// <summary>
        /// Converting String to Object
        /// </summary>
        /// <param name="iORMBase"></param>
        /// <returns></returns>
        public T StringToObject<T>(string iORMBaseString) where T : IORMBase
        {
            Type type = typeof(T);
            T iORMBase = (T)Activator.CreateInstance(type);
            string[] args = iORMBaseString.Split('\t');
            for (int a = 1; a < args.Length; a++)
            {
                string[] com = args[a].TrueSplit("=");
                var property = typeof(T).GetField(com[0]);
                if (property != null)
                {
                    property.SetValue(iORMBase, com[1]);
                }
                else
                {

                }
            }
            return iORMBase;
        }

        /// <summary>
        /// Get block by element id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        string GetBlockById(string id)
        {
            int newId = int.Parse(id);
            string lastName = LastName;
            for (int i = 0; i < blocks.Count; i++)
            {
                if (int.TryParse(blocks[i], out int firstIdInBlock))
                {
                    if (newId >= firstIdInBlock & newId < firstIdInBlock + ObjectsPerFile)
                        return blocks[i];
                }
            }
            blocks.Add(id);
            return id;
        }

        /// <summary>
        /// Return all objects
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> GetAll<T>() where T : IORMBase
        {
            string className = typeof(T).ToString();
            Type elementType = Type.GetType(className, false, true);
            if (elementType == null)
            {
                throw new Exception("ORM error");
            }
            List<T> list = new List<T>();
            for (int id = 0; id < blocks.Count; id++)
            {
                var iORMBase = ReadBlock<T>(blocks[id]);
                if (iORMBase != null)
                {
                    for (int j = 0; j < iORMBase.Count; j++)
                    {
                        list.Add(iORMBase[j] as T);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Return objects by params. Example: Id=100
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="varNval"></param>
        /// <returns></returns>
        public List<T> GetByArgs<T>(params string[] varNval) where T : IORMBase
        {
            int matchings = 0;
            List<T> list = new List<T>();
            Type type = typeof(T).GetType();
            var property = typeof(T).GetFields();
            for (int id = blocks.Count - 1; id >= 0; id--)
            {
                var iORMBase = ReadBlock<T>(blocks[id]);
                if (iORMBase != null)
                {
                    for (int obj = 0; obj < iORMBase.Count; obj++)
                    {
                        matchings = 0;
                        for (int pr = 0; pr < property.Length; pr++)
                        {
                            for (int args = 0; args < varNval.Length; args++)
                            {
                                if (varNval[args] == property[pr].Name + "=" + property[pr].GetValue(iORMBase[obj]))
                                {
                                    matchings++;
                                    break;
                                }
                            }
                        }
                        if (matchings == varNval.Length)
                        {
                            list.Add(iORMBase[obj]);
                        }
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Return all objects from block
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="BlockName"></param>
        /// <returns></returns>
        public List<T> ReadBlock<T>(string BlockName) where T : IORMBase
        {
            string className = typeof(T).ToString();
            Type elementType = Type.GetType(className, false, true);
            if (elementType == null)
            {
                throw new Exception("ORM error");
            }
            //var res = TypeDelegator.GetType("SocSet." + className, false, true);
            //Type listType = typeof(List<>).MakeGenericType(new Type[] { elementType });
            //var elementActivator = Activator.CreateInstance(elementType);
            List<T> list = new List<T>();
            string path = Folder + "\\" + BlockName + ".dicdb";
            if (File.Exists(path))
            {
                string[] allInfo;
                if (FileCache.GetFileFromCache(path, out FileCache cache))
                {
                    allInfo = cache.obj.ToString().Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    StreamReader reader = new StreamReader(path, Encoding.Unicode);
                    string fileObj = reader.ReadToEnd();
                    allInfo = fileObj.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    reader.Close();
                    FileCache.fileCaches.Add(new FileCache(path, fileObj));
                }
                for (int n = 0; n < allInfo.Length; n++)
                {
                    string[] args = allInfo[n].Split('\t');
                    if (args.Length > 1)
                    {
                        T iORMBase = (T)Activator.CreateInstance(elementType);
                        //Student student = new Student() { Id = args[0] };
                        for (int a = 0; a < args.Length; a++)
                        {
                            string[] com = args[a].TrueSplit("=");
                            var property = typeof(T).GetField(com[0]);
                            if (property != null)
                            {
                                property.SetValue(iORMBase, com[1]);
                            }
                            else
                            {

                            }
                        }
                        if (iORMBase.IsDeleted.StringToBool() == false)
                            list.Add(iORMBase);
                        //student.GetDateOfBirtn();
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Return list objects by IDs
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<T> ReadById<T>(params string[] id) where T : IORMBase
        {
            List<T> results = new List<T>();
            for (int i = 0; i < id.Length; i++)
            {
                results.Add(ReadById<T>(id[i]));
            }
            return results;
        }

        /// <summary>
        /// Return object by ID
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public T ReadById<T>(string id) where T : IORMBase
        {
            string className = typeof(T).ToString();
            Type elementType = Type.GetType(className, false, true);
            if (elementType == null)
            {
                throw new Exception("ORM error");
            }
            string BlockName = GetBlockById(id);
            string path = Folder + "\\" + BlockName + ".dicdb";
            if (true || File.Exists(path))
            {
                string[] allInfo;
                if (FileCache.GetFileFromCache(path, out FileCache cache))
                {
                    allInfo = cache.obj.ToString().Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    StreamReader reader = new StreamReader(path, Encoding.Unicode);
                    string fileObj = reader.ReadToEnd();
                    allInfo = fileObj.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    reader.Close();
                    FileCache.fileCaches.Add(new FileCache(path, fileObj));
                }
                for (int n = 0; n < allInfo.Length; n++)
                {
                    string[] args = allInfo[n].Split('\t');
                    if (args.Length > 1)
                    {
                        if (args[0] == id)
                        {
                            T iORMBase = (T)Activator.CreateInstance(elementType);
                            //Student student = new Student() { Id = args[0] };
                            for (int a = 1; a < args.Length; a++)
                            {
                                string[] com = args[a].TrueSplit("=");
                                var property = typeof(T).GetField(com[0]);
                                if (property != null)
                                {
                                    property.SetValue(iORMBase, com[1]);
                                }
                                else
                                {

                                }
                            }
                            if (iORMBase.IsDeleted.StringToBool() == false)
                                return iORMBase;
                            else return null;
                        }
                    }
                }
            }
            return default;
        }

        bool SaveInBlock(string BlockName, string id, string info)
        {
            string all = "";
            bool exist = false;
            FileCache cache = null;
            string path = Folder + "\\" + BlockName + ".dicdb";
            if (File.Exists(path))
            {
                string[] allInfo;
                if (FileCache.GetFileFromCache(path, out cache))
                {
                    allInfo = cache.obj.ToString().Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    StreamReader reader = new StreamReader(path, Encoding.Unicode);
                    string fileObj = reader.ReadToEnd();
                    allInfo = fileObj.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    reader.Close();
                    cache = new FileCache(path, fileObj);
                    FileCache.fileCaches.Add(cache);
                }
                if (allInfo.Length >= ObjectsPerFile)//check to overflow
                {
                    int startid = int.Parse(BlockName);
                    int newid = int.Parse(id);
                    if (newid - startid >= ObjectsPerFile)
                        return false;
                }

                for (int n = 0; n < allInfo.Length; n++)
                {
                    string[] args = allInfo[n].Split('\t');
                    if (args.Length > 1)
                    {
                        if (args[0] == id)
                        {
                            all += info + "\r\n";
                            exist = true;
                        }
                        else
                            all += allInfo[n] + "\r\n";
                    }
                }
                if (!exist)
                    all += info + "\r\n";
            }
            else
            {
                all += info + "\r\n";
                cache = new FileCache(path, all);
                FileCache.fileCaches.Add(cache);
            }
            cache.obj = all;
            if (!exist)
            {
                StreamWriter streamWriter = new StreamWriter(path, true, Encoding.Unicode);
                streamWriter.Write(info + "\r\n");
                streamWriter.Close();
            }
            else
            {
                StreamWriter streamWriter = new StreamWriter(path, false, Encoding.Unicode);
                streamWriter.Write(all);
                streamWriter.Close();
            }
            return true;
        }

        /// <summary>
        /// Save iORMBase-object
        /// </summary>
        /// <param name="iORMBase"></param>
        public void Save(IORMBase iORMBase)
        {
            iORMBase.OnSaving();
            if (iORMBase.Id == null)
            {
                iORMBase.Id = LastName;
                LastName = xtensions.GetNext(LastName);
            }
            var block = GetBlockById(iORMBase.Id);
            string allstr = ObjectToString(iORMBase);
            if (SaveInBlock(block, iORMBase.Id, allstr))
            {

            }
            else//In last block 100 objects yet
            {
                SaveInBlock(iORMBase.Id, iORMBase.Id, allstr);
            }
        }
    }
    public class IORMBase
    {
        public string Id;
        public string IsDeleted = "0";
        public virtual void OnSaving()
        {

        }
        public virtual void OnReaded()
        {

        }
    }
}