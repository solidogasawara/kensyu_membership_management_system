﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.Data.SqlClient;
using System.Configuration;
using System.Diagnostics;

namespace kensyu
{
    public partial class admin_register_page : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // まだログインしてないなら、ログイン画面に飛ばす
            if (Session["loginId"] == null)
            {
                Response.Redirect("~/admin-login-page.aspx");
            }
        }

        // 管理者登録
        [System.Web.Services.WebMethod]
        public static string AdminRegister(string loginId, string inputtedPassword)
        {
            // 登録しようとしているログインidが既に登録されているidじゃないかを管理するフラグ
            // 登録済みだったならtrueになる
            bool isLoginIdExist = false;

            string connectionString = ConfigurationManager.ConnectionStrings["MyConnectionString"].ConnectionString;

            // ログインidの重複確認開始
            // 処理中に例外が発生した場合、リザルトを返す
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    SqlCommand command = new SqlCommand();

                    // ログインidの重複を調べるためのSQL文
                    string query = @"SELECT COUNT(*) AS count FROM V_Admin WHERE login_id = @loginId AND delete_flag = 0";

                    command.Parameters.Add(new SqlParameter("@loginId", loginId));

                    command.CommandText = query;
                    command.Connection = connection;

                    connection.Open();

                    SqlDataReader reader = command.ExecuteReader();

                    // もしcountが1以上なら重複ありとみなす
                    if (reader.Read())
                    {
                        int loginIdCount = Convert.ToInt32(reader["count"]);

                        if (loginIdCount >= 1)
                        {
                            isLoginIdExist = true;
                        }
                    }
                } catch(Exception e)
                {
                    Debug.WriteLine(e.ToString());

                    // 不明なエラー
                    return "unexpected error";
                }
            }

            // 重複ありだったならリザルトを返して、この後の処理を実行しない
            if (isLoginIdExist)
            {
                return "loginId exists";
            }

            // idを連番にするためにデータ数を調べる
            int count = 0;

            // データ数を調べる
            // 処理中に例外が発生した場合、リザルトを返す
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    string query = "SELECT COUNT(*) AS count FROM M_Customer";
                    SqlCommand command = new SqlCommand(query, connection);

                    connection.Open();

                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        string countStr = reader["count"].ToString();
                        count = Convert.ToInt32(countStr);
                    }
                } catch(Exception e)
                {
                    Debug.WriteLine(e.ToString());

                    // 不明なエラー
                    return "unexpected error";
                }               
            }

            // 登録処理開始
            // パスワードはハッシュ化して保存する
            // 登録処理中に例外が発生した場合、それぞれのリザルトを返す
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand();

                StringBuilder sb = new StringBuilder();
                sb.Append(@"INSERT INTO M_Admin (id, role_id, login_id, salt, password, created_at)");
                sb.Append(@"VALUES (@id, @roleId, @loginId, @salt, @password, @createdAt)");

                string query = sb.ToString();

                // 連番にするため、データ数に1を足す
                int id = count + 1;

                // ソルトを取得する
                string salt = AuthenticationManager.GenerateSalt();
                // 入力されたパスワードにソルトを足したものをハッシュ化する
                string password = AuthenticationManager.HashPassword(inputtedPassword, salt);

                // 登録日
                DateTime createdAt = DateTime.Now;

                command.Parameters.Add(new SqlParameter("@id", id));
                command.Parameters.Add(new SqlParameter("@roleId", 2));
                command.Parameters.Add(new SqlParameter("@loginId", loginId));
                command.Parameters.Add(new SqlParameter("@salt", salt));
                command.Parameters.Add(new SqlParameter("@password", password));
                command.Parameters.Add(new SqlParameter("@createdAt", createdAt));

                command.CommandText = query;
                command.Connection = connection;

                connection.Open();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        command.Transaction = transaction;
                        command.ExecuteNonQuery();

                        transaction.Commit();

                        // 登録に成功したら、セッションを削除する
                        HttpContext context = HttpContext.Current;
                        context.Session.Remove("loginId");
                        context.Session.Remove("roleId");

                        // 成功
                        return "success";
                    }
                    catch (SqlException e)
                    {
                        transaction.Rollback();
                        Debug.WriteLine(e.ToString());

                        if(e.Number == 2627)
                        {
                            // 既に登録されているログインidを登録しようとした
                            return "loginId exists";
                        } else
                        {
                            // 不明なエラー
                            return "unexpected error";
                        }
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        Debug.WriteLine(e.ToString());

                        // 不明なエラー
                        return "unexpected error";
                    }
                }
            }
        }
    }
}