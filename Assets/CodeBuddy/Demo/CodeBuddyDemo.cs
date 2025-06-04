using UnityEngine;

namespace CodeBuddy.Demo
{
    public class CodeBuddyDemo : MonoBehaviour
    {
        public void JoinDiscord()
        {
            Application.OpenURL("https://discord.gg/JdsepFhEeX");
        }

        public void OpenDocumentation()
        {
            Application.OpenURL("https://docs.google.com/document/d/14pZVj2Ica7BCwL82foX2wlsVV_EbWxlWa91VPOMyoBU/");
        }
    }
}
