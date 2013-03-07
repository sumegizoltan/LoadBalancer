using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication1
{
    /// <summary>
    /// MainApp startup class for LoadBalancer
    /// Singleton Design Pattern.
    /// </summary>
    class MainApp
    {
        /// <summary>
        /// Entry point into console application.
        /// </summary>
        static void Main()
        {
            Server server = null;
            LoadBalancer b1 = LoadBalancer.GetLoadBalancer();
            LoadBalancer b2 = LoadBalancer.GetLoadBalancer();
            LoadBalancer b3 = LoadBalancer.GetLoadBalancer();
            LoadBalancer b4 = LoadBalancer.GetLoadBalancer();

            // Same instance?
            if (b1 == b2 && b2 == b3 && b3 == b4)
            {
                Console.WriteLine("Same instance\n");
            }

            // Load balance 20 server requests
            LoadBalancer balancer = LoadBalancer.GetLoadBalancer();
            for (int i = 0; i < 20; i++)
            {
                if (i > 8)
                {
                    LoadBalancer.IsRandomizeServers = false;
                }

                if (i == 9)
                {
                    balancer.AddServer("ServerVI", 1);
                    balancer.AddServer("ServerVII", 1);
                    balancer.AddServer("ServerVIII", 1);
                }

                if (i == 12)
                {
                    balancer.RemoveServer("ServerVI");
                    balancer.RemoveServer("ServerVII");
                    balancer.RemoveServer("ServerVIII");
                }

                server = balancer.Server;
                Console.WriteLine("{2} Dispatch Request to: {0} - request: {1}",
                                  server.Name,
                                  server.RequestCount,
                                  i);
            }

            Console.ReadKey();
        }
    }

    /// <summary>
    /// The 'Server' class
    /// </summary>
    public class Server
    {
        private bool isUsed = false;
        private string name = String.Empty;
        private double rate = 0.5;
        private double requestCount = 0;

        public Server(string name, double rate)
        {
            if (rate <= 0)
                throw new ArgumentOutOfRangeException();

            this.name = name;
            this.rate = rate;
        }

        private void OnJobFinished(object sender, EventArgs e)
        {
            this.RequestCount--;
        }

        public bool IsUsed { get { return this.isUsed; } set { this.isUsed = value; } }
        public string Name { get { return this.name; } set { this.name = value; } }
        public double Rate { get { return this.rate; } set { this.rate = value; } }

        public double RequestCount
        {
            get { return this.requestCount; }
            set
            {
                if (this.requestCount != value)
                {
                    this.requestCount = value;
                    this.isUsed = (this.requestCount > 0);
                }
            }
        }

        public double CalculatedRate
        {
            get { return (this.requestCount / this.rate); }
        }
    }

    /// <summary>
    /// The 'Singleton' class
    /// </summary>
    public class LoadBalancer
    {
        private static LoadBalancer _instance = null;
        private static bool _isRandomizeServers = true;
        private Random _random = null;
        private List<Server> _servers = null;

        private static object syncLock = new object();

        #region Constructor
        /// <summary>Constructor</summary>
        protected LoadBalancer()
        {
            _servers = new List<Server>();
            _servers.Add(new Server("ServerI", 0.5));
            _servers.Add(new Server("ServerII", 1));
            _servers.Add(new Server("ServerIII", 1));
            _servers.Add(new Server("ServerIV", 1.5));
            _servers.Add(new Server("ServerV", 3));

            if (_isRandomizeServers)
            {
                SetRandom();
            }
        }
        #endregion

        #region public methods

        /// <summary>
        /// Get the load balancer
        /// 
        /// Support multithreaded applications through 'Double checked locking' 
        /// pattern which (once the instance exists) avoids locking each
        /// time the method is invoked
        /// </summary>
        /// <return>Singleton instance</return>
        public static LoadBalancer GetLoadBalancer()
        {
            if (_instance == null)
            {
                lock (syncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new LoadBalancer();
                    }
                }
            }

            return _instance;
        }

        /// <summary>
        /// 
        /// </summary>
        public void AddServer(string serverName, double rate)
        {
            if (!_servers.Exists(s => s.Name == serverName))
            {
                _servers.Add(new Server(serverName, rate));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void RemoveServer(string serverName)
        {
            _servers.RemoveAll(s => s.Name == serverName);
        }
        #endregion

        /// <summary>
        /// Initialize the private field
        /// </summary>
        private void SetRandom()
        {
            if (this._random == null) this._random = new Random();
        }

        #region Properties
        /// <summary>
        /// Property for the load balancer Server selection
        /// </summary>
        public static bool IsRandomizeServers
        {
            get { return _isRandomizeServers; }
            set { _isRandomizeServers = value; }
        }

        /// <summary>
        /// Property for one of available Servers
        /// 
        /// Load balancer with request counter
        /// </summary>
        /// <return></return>
        public Server Server
        {
            get
            {
                int count = 0;
                Server server = null;
                IEnumerable<Server> servers = from s in _servers
                                              orderby s.IsUsed, s.RequestCount
                                              select s;

                if (servers != null)
                {
                    server = servers.First();

                    if (_isRandomizeServers)
                    {
                        SetRandom();
                        servers = servers.Where(s => (s.IsUsed == server.IsUsed) &&
                                                     (s.RequestCount == server.RequestCount));

                        count = servers.Count();

                        if (count > 1)
                        {
                            server = servers.ToArray()[_random.Next(count)];
                        }
                    }

                    server.RequestCount++;
                }
                else
                {
                    throw new InvalidOperationException();
                }

                return server;
            }
        }
        #endregion
    }
}
