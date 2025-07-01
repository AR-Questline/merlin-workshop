using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Awaken.TG.Editor.Utility.Audio {
  public static class WindowsVoice {
    [DllImport("WindowsVoice")]
    public static extern void initSpeech();

    [DllImport("WindowsVoice")]
    public static extern void destroySpeech();

    [DllImport("WindowsVoice")]
    public static extern void addToSpeechQueue(string s);

    [DllImport("WindowsVoice")]
    public static extern void clearSpeechQueue();

    [DllImport("WindowsVoice")]
    public static extern void statusMessage(StringBuilder str, int length);

    [DllImport("WindowsVoice")]
    public static extern void saveTextToWav(string textToRead, string filePath);

    public static void Speak(string msg) {
      initSpeech();
      addToSpeechQueue(msg);
    }

    public static void SpeakToWav(string msg, string filePath) {
      msg = "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>" +
            "<prosody rate='1.1'>" + RemoveSpecialCharacters(msg) + "</prosody></speak>";
      saveTextToWav(msg, filePath);
    }

    public static void Close() {
      destroySpeech();
    }
    
    static string RemoveSpecialCharacters(this string str) {
      StringBuilder sb = new StringBuilder();
      foreach (char c in str) {
        if (c is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '.' or ',' or '?' or ' ') {
          sb.Append(c);
        }
      }
      return sb.ToString();
    }
  }
}
