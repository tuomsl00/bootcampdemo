using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using bootdemo.Models;
using Rest;
using RestSharp;
using RestClient = RestSharp.RestClient;
using Newtonsoft.Json;
using System.Data.SqlClient;
using NewsAPI;
using NewsAPI.Models;
using NewsAPI.Constants;
using System.Data;

namespace bootdemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index(string apikey)
        {

            string connectionString = null;
            SqlConnection connection;
            SqlCommand command;
            string sql = null;
            SqlDataReader dataReader;

            connectionString = "Server= localhost\\sqlexpress; Database= News; Trusted_Connection = True; MultipleActiveResultSets=true ";

            ViewBag.apikey = apikey;

            try
            {
                connection = new SqlConnection(connectionString);
                connection.Open();
            }
            catch (Exception ex)
            {
                connectionString = "Server = localhost\\sqlexpress; Trusted_Connection = True; MultipleActiveResultSets=true";
                connection = new SqlConnection(connectionString);
                connection.Open();


                sql = "CREATE DATABASE News";
                command = new SqlCommand(sql, connection);
                command.ExecuteNonQuery();

                try
                {
                    connectionString = "Server= localhost\\sqlexpress; Database= News; Integrated Security = True; MultipleActiveResultSets=true ";
                    connection = new SqlConnection(connectionString);
                    connection.Open();
                } catch (Exception ex2)
                {
                    Debug.WriteLine("DEBUG: " + ex2.Message);
                    return View();
                }

                try
                {
                    sql = "CREATE TABLE connectionInfo (apikey varchar(255))";
                    command = new SqlCommand(sql, connection);
                    command.ExecuteNonQuery();
                } catch (Exception ex2)
                {
                    Debug.WriteLine("DEBUG: " + ex2.Message);
                    return View();

                }
                sql = "CREATE TABLE Newslist(ID int NOT NULL, Title varchar(255), Author varchar(255), Description TEXT, Url TEXT NOT NULL, UrlToImage TEXT, PublishedAt DATETIME)";
                command = new SqlCommand(sql, connection);
                command.ExecuteNonQuery();

                sql = "CREATE TABLE Words(ID int NOT NULL, Words varchar(255))";
                command = new SqlCommand(sql, connection);
                command.ExecuteNonQuery();

                return View();
            }

            try
            {
                // Read apikey from connectionInfo table
                sql = "SELECT apikey FROM connectionInfo";
                command = new SqlCommand(sql, connection);
                dataReader = command.ExecuteReader();
                dataReader.Read();
                ViewBag.apikey = (string)dataReader.GetValue(0);
                if (ViewBag.apikey == "")
                {
                    sql = "UPDATE connectionInfo SET apikey = '" + apikey + "' WHERE 1=1";
                    command = new SqlCommand(sql, connection);
                    command.ExecuteNonQuery();
                }

                return View();
            }
            catch
            {

                if (ViewBag.apikey == null)
                {
                    sql = "INSERT INTO connectionInfo (apikey) VALUES ('" + apikey + "')";
                    command = new SqlCommand(sql, connection);
                    command.ExecuteNonQuery();
                }
                else
                {

                    sql = "UPDATE connectionInfo SET apikey = '" + apikey + "' WHERE 1=1";
                    command = new SqlCommand(sql, connection);
                    command.ExecuteNonQuery();
                }
                    
            }

            return View();


        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public string searchResults(string searchTerm)
        {
            /*
            if (Request.Headers["X-Requested-With"] != "XMLHttpRequest")
            {
                Response.StatusCode = 403;
                return Response.StatusCode.ToString();
            }
            */
            SqlConnection connection;
            SqlCommand command;
            string sql = null;
            SqlDataReader dataReader;

            string connectionString = "Server= localhost\\sqlexpress; Database= News; Integrated Security = True; MultipleActiveResultSets=true ";
            connection = new SqlConnection(connectionString);
            connection.Open();
            try
            {
                sql = "SELECT * FROM Newslist WHERE Description LIKE '%" + searchTerm + "%'";
                command = new SqlCommand(sql, connection);
                dataReader = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            //No results
            if (!dataReader.Read())
            {
                sql = "SELECT apikey FROM connectionInfo";
                command = new SqlCommand(sql, connection);
                dataReader = command.ExecuteReader();
                dataReader.Read();
                ViewBag.apikey = (string)dataReader.GetValue(0);

                var newsApiClient = new NewsApiClient(ViewBag.apikey);

                DateTime curdate = DateTime.Now;
                curdate = curdate.AddDays(-30);

                var articlesResponse = newsApiClient.GetEverything(new EverythingRequest
                {
                    Q = searchTerm,
                    SortBy = SortBys.Popularity,
                    Language = Languages.EN,
                    From = new DateTime(curdate.Year, curdate.Month, curdate.Day)
                });

                if (articlesResponse.Status == Statuses.Ok)
                {
                    Debug.WriteLine("OK1");
                    // total results found
                    Console.WriteLine(articlesResponse.TotalResults);
                    // here's the first 20
                    var dt = new DataTable();
                    var Unique_words = new DataTable();

                    dt.Columns.Add("ID");
                    dt.Columns.Add("Title");
                    dt.Columns.Add("Author");
                    dt.Columns.Add("Description");
                    dt.Columns.Add("Url");
                    dt.Columns.Add("UrlToImage");
                    dt.Columns.Add("PublishedAt");

                    Unique_words.Columns.Add("ID");
                    Unique_words.Columns.Add("Words");

                    int id = 0, w_id = 0;
                    string[] nowords = { "a", "the", "an", "I", "my", "mine", "me", "your", "yours", "you", "he", "his", "his", "him", "she", "her", "hers", "her", "it", "its", "its", "it", "we", "our", "ours", "us", "they", "their", "theirs", "them" };
                    foreach (var article in articlesResponse.Articles)
                    {
                        var words = new HashSet<string>(article.Description.Split(' '));

                        //    List<string> Unique_words = new List<string>();
                        foreach (string s in words)
                        {
                            bool ignore = false;
                            foreach (string noword in nowords)
                            {
                                if (s == noword)
                                {
                                    ignore = true;
                                    break;
                                }
                            }
                            if (!ignore)
                            {
                                Unique_words.Rows.Add(w_id++, s);
                            }
                        }

                        dt.Rows.Add(id++, article.Title, article.Author, article.Description, article.Url, article.UrlToImage, article.PublishedAt);
                    }

                    using (var sqlBulk = new SqlBulkCopy(connectionString))
                    {
                        sqlBulk.DestinationTableName = "Newslist";
                        sqlBulk.WriteToServer(dt);
                        sqlBulk.DestinationTableName = "Words";
                        sqlBulk.WriteToServer(Unique_words);
                    }

                    try
                    {
                        sql = "SELECT * FROM Newslist WHERE Description LIKE '%" + searchTerm + "%'";
                        command = new SqlCommand(sql, connection);
                        dataReader = command.ExecuteReader();
                        dataReader.Read();
                    }
                    catch (Exception ex)
                    {
                        return JsonConvert.SerializeObject(ex.Message);
                    }
                } else
                {
                    return articlesResponse.Error.Message;
                }
            }

            List<News> Rows = new List<News>();
            Object[] values = new Object[dataReader.FieldCount];
            do
            {

                int fc;
                try
                {
                    fc = dataReader.GetValues(values);
                }
                catch
                {
                    break;
                }

                if (fc > 0)
                {
                    News row = new News();
                    row.ID = (int)values[0];
                    row.Title = (string)values[1];
                    row.Author = (string)values[2];
                    row.Description = (string)values[3];
                    row.Url = (string)values[4];
                    row.UrlToImage = (string)values[5];
                    row.PublishedAt = (DateTime)values[6];
                    Debug.WriteLine("title: "+row.Title);
                    Rows.Add(row);
                } 
            } while (dataReader.Read());

            connection.Close();
            Debug.WriteLine(Rows);
            return JsonConvert.SerializeObject(Rows);

            }

            public IActionResult UusiSivu()
        {

            return View();

        }
        

    }
}
