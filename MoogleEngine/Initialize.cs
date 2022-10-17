using System.Diagnostics;
namespace MoogleEngine
{
    public class Initialize
    {
         //  Diccionario donde almacenare los Docs,sus palabras y frecuencia.
        public static Dictionary<string,Dictionary<string,double>> Files = new Dictionary<string, Dictionary<string, double>>();

        //  Diccionario donde almacenare el IDF de cada palabra.
        public static Dictionary<string,double> IDF = new Dictionary<string, double>();

        //  Diccionario donde almacenare cada Doc con su texto correspondiente.
        public static Dictionary <string,string> Texts = new Dictionary<string, string>();

        public static void Feed ()
        {
            Stopwatch crono = new Stopwatch();
            crono.Start();
            Reader.Feedfiles(Files);
            Reader.TF(Files);
            Reader.FeedIDF(Files,IDF);
            Reader.Weight(Files,IDF);
            Reader.FeedTexts(Texts);
            crono.Stop();
            Console.WriteLine(crono.Elapsed);
        }
    }
}