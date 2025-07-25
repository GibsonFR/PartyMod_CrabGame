namespace PartyMod
{
    public class PartyModCoroutineRunner : MonoBehaviour
    {
        public static PartyModCoroutineRunner Instance;

        public void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
