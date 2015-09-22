using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace updateFromGit
{
    /// <summary>
    /// Класс сценария
    /// </summary>
    class Scenario
    {
        /// <summary>
        /// Создание нового сценария
        /// </summary>
        /// <param name="name">имя сценария</param>
        public Scenario(string name, string path2save)
        {
            this._name = name;
            this._gitLink = string.Empty;
            this._gitLocal = string.Empty;
            this._gitBranch = string.Empty;
            this._paths2servers = new List<string>();
            this._dateTimeLastModification = DateTime.Now;
            this._path2save = path2save;
        }
        /// <summary>
        /// Чтение сценария из XML-документа
        /// </summary>
        /// <param name="scendoc">XML со сценарием</param>        
        public Scenario(string scenarioPath)
        {
            this._path2save = scenarioPath;
            
            XDocument doc = XDocument.Load(scenarioPath);

            this._name = doc.Element("scenario").Attribute("name").Value;
            this._dateTimeLastModification = DateTime.Parse(doc.Element("scenario").Attribute("lastModified").Value);
            this._gitLink = doc.Element("scenario").Element("gitLink").Value;
            this._gitLocal = doc.Element("scenario").Element("gitLocal").Value;
            this._gitBranch = doc.Element("scenario").Element("gitBranch").Value;

            List<string> paths = new List<string>();
            foreach (var path in doc.Element("scenario").Element("paths").Descendants())
            {
                paths.Add(path.Value);
            }
            this._paths2servers = paths;
        }

        private string _name;
        /// <summary>
        /// Имя сценария
        /// </summary>
        public string name { get { return _name; } }

        private string _gitLink;
        /// <summary>
        /// Ссылка на Git репозиторий
        /// </summary>
        public string gitLink { get { return _gitLink; } }

        private string _gitLocal;
        /// <summary>
        /// Путь до локального Git репозитория
        /// </summary>
        public string gitLocal { get { return _gitLocal; } }

        private string _gitBranch;
        /// <summary>
        /// Ветвь для обновления
        /// </summary>
        public string gitBranch { get { return _gitBranch; } }

        private List<string> _paths2servers;
        /// <summary>
        /// Список путей до серверов, по которым лежит проект
        /// </summary>
        public List<string> paths2servers { get { return _paths2servers; } }
        
        private DateTime _dateTimeLastModification;
        /// <summary>
        /// Дата и время последнего изменения (или создания) сценария
        /// </summary>
        public DateTime dateTimeLastModification { get { return _dateTimeLastModification; } }
        
        private string _path2save;
        /// <summary>
        /// Путь до файла сценария
        /// </summary>
        public string path2save { get { return _path2save; } }
        
        public bool updateScenario(
            string newGitLink,
            string newLocal,
            string newBranch,
            List<string> newPaths
            )
        {
            this._dateTimeLastModification = DateTime.Now;

            this._gitLink = newGitLink;
            this._gitLocal = newLocal;
            this._gitBranch = newBranch;
            this._paths2servers.Clear();
            foreach (string path in newPaths)
            {
                this._paths2servers.Add(path);
            }

            return exportScenario2XML(path2save);
        }

        /// <summary>
        /// Сохраняет сценарий в XML файл
        /// </summary>
        /// <returns>true - сохранён; false - не сохранён</returns>
        public bool exportScenario2XML(string dirOfScenaries)
        {
            XDocument savedXML = new XDocument(
                new XElement("scenario", new XAttribute("name", name), new XAttribute("lastModified", DateTime.Now.ToString()),
                        new XElement("gitLink", gitLink),
                        new XElement("gitLocal", gitLocal),
                        new XElement("gitBranch", gitBranch),
                        new XElement("paths")
                    ));
            foreach (string path in paths2servers)
            {
                savedXML.Element("scenario").Element("paths").Add(new XElement("path", path));
            }
            try
            {
                using (TextWriter sw = new StreamWriter(
                    path2save,
                    false,
                    Encoding.GetEncoding(65001)
                    ))
                {
                    savedXML.Save(sw);
                }
                return true;
            }
            catch { return false; }
        }
    }
}
