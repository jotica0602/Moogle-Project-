using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Net.Mime;
using System.Diagnostics;
namespace MoogleEngine;


public static class Moogle
{
        //  Declaro todas mis variables, metodos y objetos a utilizar.

        //  Diccionario donde almacenare los Docs y su puntuacion.
        static Dictionary<string,double> Score = new Dictionary<string,double>();

        //  Diccionario donde almacenare las palabras de la query y sus pesos.
        static Dictionary<string,double> QueryWeight = new Dictionary<string,double>();

        //  Diccionario donde almacenare cada palabra que pueda representar una posible sugerencia a mi busqueda.
        public static Dictionary <string,Dictionary<string,int>> Suggestions = new Dictionary<string, Dictionary<string,int>>();

        public static Dictionary<string,double> sortedResults;

        public static Dictionary<string,int> sortedsuggestions;
    
    
    public static SearchResult Query(string query) 
    {
        // Llamo a los metodos de la clase Reader
        // para rellenar todos los diccionarios y realizar las 
        // operaciones pertinentes.

        Stopwatch crono = new Stopwatch();
        crono.Start();
        Reader.FeedQW(QueryWeight,Reader.Clean(query),Initialize.IDF);
        Reader.FeedScore (Initialize.Files, QueryWeight, Score);
        Reader.Operators(Reader.OPS(query),Reader.Clean(query),Initialize.Files,Score,Initialize.Texts);
        
        // Ordeno mi diccionario de Scores en orden descendente y mi diccionario de sugerencias en orden ascendente 
        var sortedResults = Score.OrderByDescending(pair => pair.Value).Take(3);
        // Solo interesan los 3 primeros resultados a mostrar o menos.   
        
        string suggestion = "";
        foreach(var word in Reader.Clean(query))
        {
            suggestion+= " "+Reader.Suggestion(word,Initialize.IDF);
        }

        SearchItem[] items = new SearchItem[sortedResults.Count()];
        for (int i =0;i<sortedResults.Count();i++)
        {
            items[i] = new SearchItem(sortedResults.ElementAt(i).Key,Reader.Snippet(Reader.Clean(query),sortedResults.ElementAt(i).Key), Math.Round(Score.ElementAt(i).Value));
        }
        
        if(query == string.Empty)
        {
            items = new SearchItem[1];
            items[0] = new SearchItem ("No ha introducido ningún criterio a buscar","",0.9f);
        }
        crono.Stop();
        Console.WriteLine(crono.Elapsed);
        return new SearchResult(items, suggestion);
        
    }
}
