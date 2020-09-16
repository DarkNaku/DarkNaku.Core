namespace DarkNaku.Core {
    public abstract class Singleton<T> where T : class, new() {
        private static T _instance = null;
        public static T Instance {
            get {
                if (_instance == null) {
                    _instance = new T();
                    (_instance as Singleton<T>).OnInstantiate();
                }

                return _instance;
            }
        }

        protected virtual void OnInstantiate() { 
        }
    }
}