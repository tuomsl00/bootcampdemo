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

        //    connectionString = "Server= localhost\\sqlexpress; Database= NHLTeams; Integrated Security = True; MultipleActiveResultSets=true ";
            connectionString = "Server= localhost\\sqlexpress; Database= News; Trusted_Connection = True; MultipleActiveResultSets=true ";

            ViewBag.apikey = apikey;

            try
            {
                connection = new SqlConnection(connectionString);
                connection.Open();
            }
            catch (Exception ex)
            {
                // Create NHLTeams database
                connectionString = "Server = localhost\\sqlexpress; Trusted_Connection = True; MultipleActiveResultSets=true";
                connection = new SqlConnection(connectionString);
                connection.Open();


                sql = "CREATE DATABASE News";
                command = new SqlCommand(sql, connection);
                command.ExecuteNonQuery();

                //    CONSTRAINT UC_article UNIQUE(ID, url)

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
                sql = "CREATE TABLE News(ID int NOT NULL, Title varchar(255), Author varchar(255), Description TEXT, Url TEXT NOT NULL, UrlToImage TEXT, PublishedAt DATETIME)";
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

                sql = "SELECT * FROM TeamInfo";

                

            //    TeamList team = getJSONFeed(ViewBag.apikey);

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
            //(string)dataReader.GetValue(0);

         //List<Team> teams = new List<Team>();

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
        /*
        public IActionResult UusiSivu()
        {
            return View();
        }
        */
       

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
                sql = "SELECT * FROM News WHERE Description LIKE '%" + searchTerm + "%'";
                command = new SqlCommand(sql, connection);
                dataReader = command.ExecuteReader();

            } catch (Exception ex)
            {
                return ex.Message;
            }
            //No results
            if (!dataReader.Read())
            {
                
                var newsApiClient = new NewsApiClient("a559cff68e4f4696b817a10b8f15be5d");
                var articlesResponse = newsApiClient.GetEverything(new EverythingRequest
                {
                    Q = searchTerm,
                    SortBy = SortBys.Popularity,
                    Language = Languages.EN,
                    From = new DateTime(2020, 3, 12)
                });

                if (articlesResponse.Status == Statuses.Ok)
                {
                    // total results found
                    Console.WriteLine(articlesResponse.TotalResults);
                    // here's the first 20
                    var dt = new DataTable();
                    dt.Columns.Add("ID");
                    dt.Columns.Add("Title");
                    dt.Columns.Add("Author");
                    dt.Columns.Add("Description");
                    dt.Columns.Add("Url");
                    dt.Columns.Add("UrlToImage");
                    dt.Columns.Add("PublishedAt");

                    int id = 0;
                    foreach (var article in articlesResponse.Articles)
                    {
                        dt.Rows.Add(id++, article.Title, article.Author, article.Description, article.Url, article.UrlToImage, article.PublishedAt);
                    }

                    using (var sqlBulk = new SqlBulkCopy(connectionString))
                    {
                        sqlBulk.DestinationTableName = "News";
                        sqlBulk.WriteToServer(dt);
                    }
                    /*
                    sql = Uri.EscapeDataString("INSERT INTO News (Title, Author, Description, Url, UrlToImage, PublishedAt) VALUES ('" + article.Title + "', '" + article.Author + "', '" + article.Description + "', '" + article.Url + "', '" + article.UrlToImage + "', '" + article.PublishedAt + "')");
                        Debug.WriteLine("SQL: " + sql);
                        command = new SqlCommand(sql, connection);
                        command.ExecuteNonQuery();
                    */

                    try
                    {
                        sql = "SELECT * FROM News WHERE Description LIKE '%" + searchTerm + "%'";
                        command = new SqlCommand(sql, connection);
                        dataReader = command.ExecuteReader();

                    }
                    catch (Exception ex)
                    {
                        return JsonConvert.SerializeObject(ex.Message);
                    }
                }
            }


            List<News> Rows = new List<News>();
        //    List<Object> Rows = new List<Object>();
            Object[] values = new Object[dataReader.FieldCount];
            Object rows;
            NewsObject newss;
            
            while (dataReader.Read())
            {
                int fc = dataReader.GetValues(values);
                
                News row = new News();
                row.ID = (int)values[0]; // (int)dataReader.GetValue(0);
                row.Title = (string)values[1];// (string)dataReader.GetValue(1);
                row.Author = (string)values[2];// (string)dataReader.GetValue(2);
                row.Description = (string)values[3];// (string)dataReader.GetValue(3);
                row.Url = (string)values[4];//  (string)dataReader.GetValue(4);
                row.UrlToImage = (string)values[5]; // (string)dataReader.GetValue(5);
                row.PublishedAt = (DateTime)values[6];// (DateTime)dataReader.GetValue(6);
                
                //    dataReader.Ne
            //    row = (News[])values;
                Rows.Add(row);

            }
            connection.Close();

            Debug.WriteLine(Rows[0].ID);
            Debug.WriteLine(Rows[1].ID);
            Debug.WriteLine(Rows[2].ID);

            return JsonConvert.SerializeObject(Rows);

            }

            public IActionResult UusiSivu()
        {

            return View();


            // If NHLTeams-database not found, create one.
            /*
                sql = "CREATE DATABASE NHLTeams";
                command = new SqlCommand(sql, connection);
                */
            
            /*
            sql = "SELECT * FROM Customers";

            try
            {
                connection.Open();
                command = new SqlCommand(sql, connection);
                dataReader = command.ExecuteReader();
                dataReader.Read();
                return (string)dataReader.GetValue(0);
            } catch (Exception ex) {
                return ex.Message;
            }
            */

        }
        

    }
}
