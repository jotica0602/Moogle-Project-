using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.SymbolStore;
using System.Transactions;
using System.Net.Sockets;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using System.Net.Mime;
using System.Runtime.CompilerServices;
namespace MoogleEngine
{
    public class Reader
    {
        // Redirijo la ruta del programa hacia la carpeta content
        public static string path = @"..\Content";

        // Creo un array con todos los Docs de mi ruta.
        public static string [] files = Directory.GetFiles(path);

        // Consiste en recibir un texto, llevarlo todo a minusculas,
        // eliminar los caracteres especiales y por ultimo crear un array
        // almacenando cada palabra en una posicion.

        public static string [] Clean(string text)
        {   
            char [] charstoremove = {'~','`','!','@','#','$','%','^','&','*','(',')','-','=','_','+','[',']','{','}',',','<','>','.',';',':',' ','ª','º','?','\n','\t','«','»','\r'};
            string lowertext = text.ToLower();
            string [] bow = lowertext.Split(charstoremove,StringSplitOptions.RemoveEmptyEntries); 
            // crea el array de palabras del texto.
            return bow;
        }

        // Consiste en recibir un texto, llevarlo todo a minusculas,
        // eliminar los caracteres especiales exceptuando los operadores y por ultimo crear un array
        // almacenando cada palabra en una posicion.
        public static string [] OPS(string text)
        {   
            char [] charstoremoveq = {'`','@','#','$','%','&','(',')','-','=','_','+','[',']','{','}',',','<','>','.',';',':',' ','ª','º','?'};
            string lowertext = text.ToLower();
            string [] bow = text.Split(charstoremoveq,StringSplitOptions.RemoveEmptyEntries);
            return bow;
            
        }

        // Recibe el texto de un archivo y le aplica el metodo clean,
        // devolviendo un array de palabras
        public static string [] Tobow(string file)
        {
            string text = File.ReadAllText(file);
            string [] bow = Clean(text);
            return bow;
        }
        
        // Dada una palabra, determina cuantas veces se
        // repite el caracter '*'.

        public static double Charcounter (string word)
        {
            double cont = 0;
            foreach(var character in word)
            {
                if (character=='*')
                {
                    cont++;
                }
            }
            return cont;
        }

        // Agrega al diccionario los datos de la siguiente forma:
        // <Doc,palabras,frecuencia>.

        public static void Feedfiles (Dictionary<string,Dictionary<string,double>> Files)
        {
            Files.Clear();
            foreach(var file in files)
            {
                Files.Add(file,new Dictionary<string, double>());
                foreach (var word in Tobow(file))
                {
                    if (!Files[file].ContainsKey(word))             
                    {
                        //  Si el doc no tiene esa palabra la agregara 
                        //  con 1 en su valor de frecuencia
                        Files[file].Add(word,1);   
                    }
                    else
                    {
                        //  En caso de contenerla aumentara
                        //  en 1 su valor de frecuencia
                        Files[file][word]++;
                    }
                }
            }
        }


        // Comienza la Recolecta de datos para el modelo de Espacio Vectorial

        // Recibe un Diccionario con la estructura <DOc,palabras,frecuencia> y calcula la Frecuencia Normalizada (TF)
        // de cada una de sus palabras en el texto correspondiente a cada una
        // TF = WF/MaxFreqW
        // WF --> Frecuencia de la palabra
        // MaxFreqW --> palabra de mayor frecuencia en el texto correspondiente .
        public static void TF (Dictionary<string,Dictionary<string,double>> Files)
        {
            foreach (var file in Files)
            {
                double maxfreqword =0;
                foreach(var minidict in file.Value)
                {
                    maxfreqword = Math.Max(minidict.Value,maxfreqword); 
                    // Itero buscando la palabra de mayor frecuencia por texto.
                }
                
                foreach(var minidict in file.Value)
                {
                    Files[file.Key][minidict.Key]= Files[file.Key][minidict.Key]/maxfreqword; 
                    // Itero buscando la frecuencia de cada palabra y dividiendola por la de 
                    // mayor frecuencia en su texto correspondiente.
                }
            }
        }


        // Recibe dos diccionarios y agrega al segundo "todas las palabras de todos los textos
        // al segundo sin repetir", teniendo en cuenta si la palabra habia aparecido en algun 
        // documento con anterioridad, de ser asi aumentaba el contador de repeticiones de esa palabra.
        // Finalmente calcula su IDF.
        // IDF = Log10(N/ni)
        // N --> Total de DOcs
        // ni --> Total de Docs en los que aparece la palabra.
        public static void FeedIDF (Dictionary<string,Dictionary<string,double>> Files, Dictionary<string,double> IDF)
        {
            IDF.Clear();
            // Primera parte del metodo: Agregar las palabras 
            // al diccionario teniendo en cuenta si han aparecido
            // con anterioridad en los cuerpos de otros documentos o no.
            
            foreach(var file in Files)
            {
                foreach(var minidict in file.Value)
                {
                    if (!IDF.ContainsKey(minidict.Key))
                    {
                        IDF.Add(minidict.Key,1); //Si no esta agregala con 1 una repeticion.
                    }
                    else
                    {
                        IDF[minidict.Key]++; // Si esta sumale 1 a su valor.
                    }
                }
            }

            // Calculo el IDF de cada palabra.
            foreach(var word in IDF)
            {
                IDF[word.Key]=Math.Log10(files.Length/IDF[word.Key]);
            }
        }


        // Recibe dos diccionarios como parametros y devuelve 
        // el peso (W) de cada palabra.
        // W=TF*IDF.
        public static void Weight(Dictionary<string,Dictionary<string,double>> Files,Dictionary<string,double> IDF)
        {
            foreach(var file in Files)
            {
                foreach (var minidict in file.Value)
                {
                    if (IDF.ContainsKey(minidict.Key))
                    {
                        Files[file.Key][minidict.Key] = Files[file.Key][minidict.Key]*IDF[minidict.Key];
                    }
                }
            }
        }

        // Agrega al diccionario las palabras de la query y calcula su peso (QW)
        // QW = alpha+(1-alpha)*(WF/MaxFreqWord)

        public static void FeedQW (Dictionary<string,double> QW, string[]query, Dictionary<string,double> IDF)
        {
            double alpha = 0.4;

            // Agrega las palabras sin repetir con su frecuencia 
            // utilizando el metodo Counter.

            QW.Clear();   
            foreach(var word in query)
            {
                if(!QW.ContainsKey(word))
                {
                    QW.Add(word,1);
                }
                else
                {
                    QW[word]++;
                }
            }

            // Determinamos la palabra de mayor frecuencia 
            // iterando por cada valor.

            double maxfreqword = 0;

            foreach(var word in QW)
            {
                maxfreqword = Math.Max(QW[word.Key], maxfreqword);
                
            }
            
            // Calculamos el peso iterando por la frecuencia de cada palabra
            // y dividiendo su valor entre la palabra de mayor frecuencia.

            foreach(var word in QW)
            {
                if(IDF.ContainsKey(word.Key))
                {
                    QW[word.Key] = QW[word.Key] = (alpha+(1-alpha)*(word.Value/maxfreqword))*IDF[word.Key];
                }
            }
        }

        // Agrega a un diccionario los Docs y su puntuacion (Score) con respecto 
        // a la busqueda realizada.
        // Para ello se utilizo la formula de similitud de cosenos

        public static void FeedScore(Dictionary<string,Dictionary<string,double>> Files, Dictionary<string,double> QW, Dictionary<string,double> Score)
        {
            Score.Clear();
            foreach (var file in Files)
            {
                double numerador = 0, denominador = 0, d1 = 0, d2 = 0, score = 0;

                foreach (var minidict in file.Value)
                {
                    if (QW.ContainsKey(minidict.Key))
                    {
                        numerador += Files[file.Key][minidict.Key]*QW[minidict.Key];
                        d1+=Math.Pow(Files[file.Key][minidict.Key],2);
                        d2+=Math.Pow(QW[minidict.Key],2);
                    }
                    else 
                    {
                        d1+=Math.Pow(Files[file.Key][minidict.Key],2);
                    }

                    denominador= Math.Sqrt(d1)*Math.Sqrt(d2);
                    if(denominador ==0)
                    {
                        score = 0;
                    }
                    else
                    {
                        score = numerador/denominador;
                    }
                }
                Score.Add(file.Key,score);
                
            }

            // Elimino todos los docs con score = 0;
            foreach (var file in Score)
            {
                if(file.Value==0)
                {
                    Score.Remove(file.Key);
                }
            }
        } // Fin de la recolecta de datos del Espacio Vectorial

        // Metodo para rellenar un diccionario con documentos TXT y
        // sus respectivos textos.

        //Trabajo con Operadores
        public static void Operators(string [] queryops,string [] cleanquery, Dictionary<string,Dictionary<string,double>> Files, Dictionary<string,double> Score, Dictionary<string,string> Texts)
        {
            for (int i =0; i<queryops.Length;i++)
            {
                
                if (queryops[i].StartsWith('!'))
                {
                    foreach(var file in Files)
                    {
                        if (Files[file.Key].ContainsKey(cleanquery[i]))
                        {
                            //  Analizo cada documento de mi diccionario Files
                            //  en caso de contener la palabra, remuevo el doc
                            //  de mi diccionario de Scores.
                            Score.Remove(file.Key);
                        }
                    }
                }

                if (queryops[i].StartsWith('^'))
                {
                    foreach(var file in Files)
                    {
                        if (!Files[file.Key].ContainsKey(cleanquery[i]))
                        {
                            //  Analizo cada documento de mi diccionario Files
                            //  en caso de NO contener la palabra, remuevo el doc
                            //  de mi diccionario de Scores.
                            Score.Remove(file.Key);
                        }
                    }
                }

                if (queryops[i].StartsWith('*'))
                {
                    foreach(var file in Files)
                    {
                        if (Files[file.Key].ContainsKey(cleanquery[i]) && Score.ContainsKey(file.Key))
                        {
                            //  Analizo cada documento de mi diccionario Files
                            //  en caso de contener la palabra, aumento el valor
                            //  de su puntuacion en el doble de la cantidad de veces
                            //  que aparezca el caracter '*' repetido.
                            Score[file.Key] = Score[file.Key]+(Reader.Charcounter(queryops[i])*2);
                        }
                    }
                }

                if (queryops[i].StartsWith('~'))
                {
                    if (i==0)
                    {
                        //  Si la palabra con el operador '~' esta
                        //  en la primera posicion de la query
                        //  rompo el ciclo y prosigue con las demas
                        //  acciones.
                        break;
                    }
                    else
                    {
                        //  De no ocurrir lo contrario itero por cada 
                        //  texto de mi diccionario Texts y reviso si
                        //  contiene la palabra con el operador '~' y 
                        //  y la palabra anterior a ella.
                        foreach(var file in Texts)
                        {
                            string w1 = cleanquery[i], w2 = cleanquery[i-1];
                            if(file.Value.Contains(w1) && file.Value.Contains(w2))
                            {
                                //  En caso de que la expresion evalue True
                                //  Hallo la distancia entre las palabras en el texto
                                //  con el metodo Distance y aumento en 10 la puntuacion
                                //  del doc en Score.   
                                int d = Reader.Distance(file.Value, w1, w2);
                                Score[file.Key] = Score[file.Key]+10;
                            }
                        }
                    }
                }
            }
        }

        // Almaceno todos los documentos con sus textos.
        public static void FeedTexts (Dictionary<string,string> Texts)
        {
            Texts.Clear();
            foreach (var file in files)
            {
                Texts.Add(file,File.ReadAllText(file).ToLower());
            }
        }

        // Metodo para calcular la distancia entre dos palabras en un texto
        public static int Distance(string text,string w1,string w2)
        {
            
            if (w1.Equals(w2))
            {
                return 0 ; // Si ambas palabras son iguales no hay distancia entre ellas 
            }

            string[] words = text.Split(" "); 
        
            int min_dist = (words.Length) + 1;
        
            for (int i = 0;i < words.Length ; i ++)
            {
                if (words[i].Equals(w1))
                {
                    for (int j = 0; j < words.Length; j++)
                    {
                        if (words[j].Equals(w2))
                        {
                            int curr = Math.Abs(i - j) - 1;

                            if (curr < min_dist)
                            {
                                min_dist = curr ;
                            }
                        }
                    }
                }
            }
            return min_dist;
        } // Fin del trabajo con operadores

        // Este metodo esta basado en el algoritmo de Levenshtein.
        // Consiste en recibir dos palabras y su longitud para calcular
        // la cantidad minima de operaciones a realizar para transformar una palabra en otra
        // Mientras menor sea el numero de transformaciones, mas parecidas seran las palabras.
        public static int EditDistance (string w1, string w2, int m, int n)
        {
            
            // Creo un metodo para comparar variables 
            // devolviendo la menor de 3 en cada caso.
            
            static int min(int x, int y, int z)
            {
                if (x <= y && x <= z)
                {
                    return x;
                }
                
                if (y <= x && y <= z)
                {
                    return y;
                }

                else
                {
                    return z;
                }
            }
            
            // Creo una tabla para almacenar
            // los resultados.

            int[,] tabla = new int[m + 1, n + 1];
            
    
            // Relleno la tabla haciendo un bottom-up

            for (int i = 0; i <= m; i++) 
            {
                for (int j = 0; j <= n; j++) 
                {
                    // Si la primera opcion es vacia entonces
                    // insertas todos los caracteres en el 
                    // segundo string.
                    if (i == 0)
                    {
                        // Operaciones minimas = j.
                        tabla[i, j] = j;
                    }
    
                    // Si el segundo string esta vacio, la opcion es 
                    // eliminar todos los caracteres del segundo string .

                    else if (j == 0)
                    {
    
                        // Operaciones minimas = i.
                        tabla[i, j] = i;
                    }
    
                    // Si las ultimas letras son igulas
                    // avanzamos en el string restante.

                    else if (w1[i - 1] == w2[j - 1])
                    {
                        tabla[i, j] = tabla[i - 1, j - 1];
                    }
    
                    // Si el ultimo caracter es diferente
                    // tendremos en cuenta todas las posibilidades.
                    else
                    {
                        tabla[i, j] = 1
                                        + min   (tabla[i, j - 1], // Agregar
                                                tabla[i - 1, j], // Eliminar
                                                tabla[i - 1, j - 1]); // Reemplazar.
                    }

                }
            }
            return tabla[m,n];
        }

        //  Sugerencia
        public static string Suggestion(string word, Dictionary<string,double> IDF)
        {   
            int favdistance = int.MaxValue;
            int distance;
            string favword = word;
            foreach(var w in IDF)
            {
                distance=EditDistance(w.Key,word,w.Key.Length,word.Length);
                if((distance<favdistance))
                {
                    favdistance = distance;
                    favword=w.Key;
                }
            } 
            return favword;
        }
        //  Snippet xd
        public static string Snippet (string [] query, string file)
        {   
            static string [] Normalize (string file)
            {
                string text = File.ReadAllText(file);
                char [] charstoremove = {'~','`','!','@','#','$','%','^','&','*','(',')','-','=','_','+','[',']','{','}',',','<','>','.',';',':',' ','ª','º','?','\n','\t','«','»','\r'};
                string [] bow = text.Split(charstoremove,StringSplitOptions.RemoveEmptyEntries);
                return bow; 
            }
            
            string [] text= Normalize(file);
            string snippet = "";
            for (int i = 0;i<query.Length;i++)
            {
                for(int j = 0;j<text.Length;j++)
                {
                    if(query[i]==text[j].ToLower())
                    {
                        int index = j;
                        if(text.Length !=1 && !(index+5>text.Length))
                        {
                            var segment = new ArraySegment<string>(text,index,5);
            
                            foreach (var word in segment)
                            {
                                snippet+= " "+word;
                            }
                            break;
                        }
                        else 
                        {
                            snippet = text[j];
                            break;
                        }
                    }
                }
            }
            return snippet;
        }
    }
}



