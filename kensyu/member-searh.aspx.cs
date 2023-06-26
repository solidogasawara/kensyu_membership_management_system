﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using Newtonsoft.Json;
using System.Text;
using System.Diagnostics;
using System.Web.Script.Serialization;

namespace kensyu
{
    public partial class membersearh : Page
    {

        protected void Page_Load(object sender, EventArgs e)
        {
            

            if (!IsPostBack)
            {

            }
        }

        [System.Web.Services.WebMethod]
        public static string SearchButton_Click(string idStr, string emailStr, string nameStr, string nameKanaStr, string birthStartStr, string birthEndStr, string prefectureStr, string genderStr, string memberStatusStr)
        {
            List<List<string>> tableData = SearchCustomer(idStr, emailStr, nameStr, nameKanaStr, birthStartStr, birthEndStr, prefectureStr, genderStr, memberStatusStr);

            JavaScriptSerializer js = new JavaScriptSerializer();

            // Listをjsonの形にする
            string json = js.Serialize(tableData);

            return json;
        }

        private static List<List<string>> SearchCustomer(string idStr, string emailStr, string nameStr, string nameKanaStr, string birthStartStr, string birthEndStr, string prefectureStr, string genderStr, string memberStatusStr)
        {
            // 入力されたパラメータを取得する

            // 全件表示フラグ(性別や会員状態のチェックボックスに両方チェックが入った場合全件表示にする)
            bool dispAll = false;

            int id = -1; // 会員ID(初期値は-1)

            // ID検索欄に文字列が入力されているなら、int型に変換してidに代入する
            if (!String.IsNullOrEmpty(idStr))
            {
                id = Convert.ToInt32(idStr);
            }

            string email = emailStr; // メールアドレス
            string name = nameStr; // 名前(漢字)
            string nameKana = nameKanaStr; // 名前(かな)

            // 誕生日検索
            DateTime birthStart = DateTime.Now; // 始めの日付(初期値は検索実行した時の時刻)
            DateTime birthEnd = DateTime.Now; // 終わりの日付(初期値は検索実行した時の時刻)

            // 誕生日検索欄(始めの日付)に入力がされていれば、DateTime型にParseしてbirthStartに代入する
            if (!String.IsNullOrEmpty(birthStartStr))
            {
                birthStart = DateTime.Parse(birthStartStr);
            }

            // 誕生日検索欄(終わりの日付)に入力がされていれば、DateTime型にParseしてbirthStartに代入する
            if (!String.IsNullOrEmpty(birthEndStr))
            {
                birthEnd = DateTime.Parse(birthEndStr);
            }

            // 性別(パラメータ: 1 = 男性, 2 = 女性)
            bool gender = false; // 性別(false = 男性, true = 女性)
            bool isEmptyGender = true; // 性別のパラメータの中身があるかを管理するフラグ(初期値はtrue)

            // 男性、女性どちらかのチェックボックスにチェックが入っていて、
            // 性別のパラメータに中身があるならフラグをfalseにする
            if (!String.IsNullOrEmpty(genderStr))
            {
                // パラメータに中身が入っていたので、フラグをfalseにする
                isEmptyGender = false;

                // 男性、女性のどちらのチェックボックスにもチェックが入っていた場合、
                // 「both」が渡される
                if (genderStr == "both")
                {
                    dispAll = true;
                }
                else
                {
                    // 受け取ったパラメータをint型に変換する
                    int genderNumber = Convert.ToInt32(genderStr);

                    // パラメータは1が男性、2が女性を表しているため、genderNumberが2だった場合は
                    // genderをtrueにする
                    if (genderNumber == 2)
                    {
                        gender = true;
                    }
                }
            }

            int prefecture_id = -1; // 都道府県(初期値は-1)

            // 都道府県が指定されていたなら、int型に変換してprefecture_idに代入する
            if (!String.IsNullOrEmpty(prefectureStr))
            {
                prefecture_id = Convert.ToInt32(prefectureStr);
            }

            // 会員状態(パラメータ: 1 = 有効, 2 = 退会)
            bool membershipStatus = false; // 会員状態(false = 退会, true = 有効)
            bool isEmptyMembershipStatus = true;

            // 有効、無効どちらかのチェックボックスにチェックが入っていて、
            // 会員状態のパラメータに中身があるならフラグをfalseにする
            if (!String.IsNullOrEmpty(memberStatusStr))
            {
                // パラメータに中身が入っていたので、フラグをfalseにする
                isEmptyMembershipStatus = false;

                // 有効、無効のどちらのチェックボックスにもチェックが入っていた場合、
                // 「both」が渡される
                if (memberStatusStr == "both")
                {
                    dispAll = true;
                }
                else
                {
                    // 受け取ったパラメータをint型に変換する
                    int membershipStatusNumber = Convert.ToInt32(memberStatusStr);

                    // パラメータは1が有効、2が無効を表しているため、membershipStatusNumberが1だった場合は
                    // membershipStatusをtrueにする
                    if (membershipStatusNumber == 1)
                    {
                        membershipStatus = true;
                    }
                }
            }

            string connectionString = ConfigurationManager.ConnectionStrings["MyConnectionString"].ConnectionString;
            //string query = "SELECT * FROM V_Customer";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand();

                // SQL文を作成する
                StringBuilder sb = new StringBuilder();
                sb.Append(@"SELECT c.id, name, name_kana, mail, birthday, gender, p.prefecture, membership_status FROM V_Customer AS c");
                sb.Append(@"  JOIN M_Prefecture AS p");
                sb.Append(@"    ON c.prefecture_id = p.id");
                sb.Append(@" WHERE delete_flag = 'FALSE'");

                // 全件表示フラグがfalseの時だけ、検索条件を追加していく
                if (!dispAll)
                {
                    // 検索条件(id)
                    sb.Append(@"   AND (c.id = @id");
                    command.Parameters.Add(new SqlParameter("@id", id));

                    // nameの中身が空なら検索条件にnameを含めない
                    if (!String.IsNullOrEmpty(name))
                    {
                        // 検索条件(name)
                        sb.Append(@"    OR name LIKE @name");
                        command.Parameters.Add(new SqlParameter("@name", "%" + name + "%"));
                    }

                    // nameKanaの中身が空なら検索条件にname_kanaを含めない
                    if (!String.IsNullOrEmpty(nameKana))
                    {
                        // 検索条件(name_kana)
                        sb.Append(@"    OR name_kana LIKE @nameKana");
                        command.Parameters.Add(new SqlParameter("@nameKana", "%" + nameKana + "%"));
                    }

                    // 検索条件(mail)
                    sb.Append(@"    OR mail = @email");
                    command.Parameters.Add(new SqlParameter("@email", email));

                    // 検索条件(birthday)
                    sb.Append(@"    OR birthday BETWEEN @birthStart AND @birthEnd");
                    command.Parameters.Add(new SqlParameter("@birthStart", birthStart));
                    command.Parameters.Add(new SqlParameter("@birthEnd", birthEnd));

                    // 性別のパラメータが空なら検索条件にgenderを含めない
                    if (!isEmptyGender)
                    {
                        // 検索条件(gender)
                        sb.Append(@"    OR gender = @gender");
                        command.Parameters.Add(new SqlParameter("@gender", gender));
                    }

                    // 検索条件(prefecture_id)
                    sb.Append(@"    OR prefecture_id = @prefecture");
                    command.Parameters.Add(new SqlParameter("@prefecture", prefecture_id));

                    // 会員状態のパラメータが空なら検索条件にmembership_statusを含めない
                    if (!isEmptyMembershipStatus)
                    {
                        // 検索条件(membership_status)
                        sb.Append(@"    OR membership_status = @membershipStatus");
                        command.Parameters.Add(new SqlParameter("@membershipStatus", membershipStatus));
                    }

                    sb.Append(@")");
                }

                // 作成したsqlをstring型にする
                string query = sb.ToString();

                // クエリとコネクションを指定する
                command.CommandText = query;
                command.Connection = connection;

                connection.Open();

                List<List<string>> customerData = new List<List<string>>();

                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    //c.id, name, name_kana, mail, birthday, gender, p.prefecture, membership_status
                    List<string> row = new List<string>();

                    row.Add(reader["id"].ToString());
                    row.Add(reader["name"].ToString());
                    row.Add(reader["name_kana"].ToString());
                    row.Add(reader["mail"].ToString());
                    row.Add(reader["birthday"].ToString());
                    row.Add((bool)reader["gender"] ? "女性" : "男性");
                    row.Add(reader["prefecture"].ToString());
                    row.Add((bool)reader["membership_status"] ? "有効" : "退会");

                    customerData.Add(row);
                }

                reader.Close();

                return customerData;
            }
        }

        [System.Web.Services.WebMethod]
        public static string CSVDownloadButton_Click(string idStr, string emailStr, string nameStr, string nameKanaStr, string birthStartStr, string birthEndStr, string prefectureStr, string genderStr, string memberStatusStr)
        {
            string csv = GenerateCustomerDataCSV(idStr, emailStr, nameStr, nameKanaStr, birthStartStr, birthEndStr, prefectureStr, genderStr, memberStatusStr);

            return csv;
        }

        private static string GenerateCustomerDataCSV(string idStr, string emailStr, string nameStr, string nameKanaStr, string birthStartStr, string birthEndStr, string prefectureStr, string genderStr, string memberStatusStr)
        {
            List<List<string>> customerData = SearchCustomer(idStr, emailStr, nameStr, nameKanaStr, birthStartStr, birthEndStr, prefectureStr, genderStr, memberStatusStr);

            StringBuilder sb = new StringBuilder("id,名前,名前(かな),メールアドレス,生年月日,性別,都道府県,会員状態" + "\r\n");

            foreach (List<string> values in customerData)
            {
                string line = string.Join(",", values);

                sb.Append(line + "\r\n");
            }

            string csv = sb.ToString();

            return csv;
        }

        [System.Web.Services.WebMethod]
        public static string CSVUploadButton_Click(string csv)
        {
            List<string> errorMsgs = new List<string>();

            string[] separator = new string[] { "\r\n", "\n" };
            string[] csvRows = csv.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            string connectionString = ConfigurationManager.ConnectionStrings["MyConnectionString"].ConnectionString;

            string header = "id,名前,名前(かな),メールアドレス,生年月日,性別,都道府県,会員状態";

            int rowCount = 0;

            foreach (string row in csvRows)
            {
                rowCount++;

                if (row == header)
                {
                    continue;
                }

                string[] cols = row.Split(new string[] { "," }, StringSplitOptions.None);

                string idStr = cols[0];
                int id = -1;

                if(!int.TryParse(idStr, out id))
                {
                    errorMsgs.Add(InsertError.GenerateErrorMsg(InsertError.E008_ID_ILLEGAL, rowCount));
                    continue;
                }

                string name = cols[1];
                string nameKana = cols[2];
                string email = cols[3];

                string birthdayStr = cols[4];
                DateTime birthday = DateTime.Now;

                if(!DateTime.TryParse(birthdayStr, out birthday))
                {
                    errorMsgs.Add(InsertError.GenerateErrorMsg(InsertError.E004_DATETIME_ILLEGAL, rowCount));
                    continue;
                }

                string genderStr = cols[5];
                int gender = -1;

                if(genderStr == "男性")
                {
                    gender = 0;
                } else if (genderStr == "女性")
                {
                    gender = 1;
                } else
                {
                    errorMsgs.Add(InsertError.GenerateErrorMsg(InsertError.E005_GENDER_ILLEGAL, rowCount));
                    continue;
                }

                string prefectureStr = cols[6];
                int prefectureId = -1;

                using(SqlConnection connection = new SqlConnection(connectionString))
                {
                    try
                    {
                        SqlCommand command = new SqlCommand();

                        string query = "SELECT id FROM V_Prefecture WHERE prefecture = @prefecture";

                        command.Parameters.Add(new SqlParameter("@prefecture", prefectureStr));

                        command.CommandText = query;
                        command.Connection = connection;

                        connection.Open();

                        SqlDataReader reader = command.ExecuteReader();

                        if(reader.Read())
                        {
                            prefectureId = (int) reader["id"];
                        } else
                        {
                            errorMsgs.Add(InsertError.GenerateErrorMsg(InsertError.E006_PREFECTURE_NOT_EXIST, rowCount));
                            continue;
                        }
                    } catch(Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                        errorMsgs.Add(InsertError.GenerateErrorMsg(InsertError.E1000_UNEXPECTED_ERROR, rowCount));
                        continue;
                    }
                }

                DateTime createdAt = DateTime.Now;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    try
                    {
                        SqlCommand command = new SqlCommand();

                        StringBuilder sb = new StringBuilder();
                        sb.Append(@"INSERT INTO M_Customer (id, name, name_kana, mail, birthday, gender, prefecture_id, created_at)");
                        sb.Append(@"VALUES (@id, @name, @nameKana, @email, @birthday, @gender, @prefectureId, @createdAt)");

                        command.Parameters.Add(new SqlParameter("@id", id));
                        command.Parameters.Add(new SqlParameter("@name", name));
                        command.Parameters.Add(new SqlParameter("@nameKana", nameKana));
                        command.Parameters.Add(new SqlParameter("@email", email));
                        command.Parameters.Add(new SqlParameter("@birthday", birthday));
                        command.Parameters.Add(new SqlParameter("@gender", gender));
                        command.Parameters.Add(new SqlParameter("@prefectureId", prefectureId));
                        command.Parameters.Add(new SqlParameter("@createdAt", createdAt));

                        command.CommandText = sb.ToString();
                        command.Connection = connection;

                        connection.Open();

                        using (SqlTransaction transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                command.Transaction = transaction;
                                command.ExecuteNonQuery();

                                transaction.Commit();
                            }
                            catch (SqlException e)
                            {
                                transaction.Rollback();
                                Debug.WriteLine(e.ToString());

                                switch(e.Number)
                                {
                                    case 8115:
                                        errorMsgs.Add(InsertError.GenerateErrorMsg(InsertError.E001_ID_OVERFLOW, rowCount));
                                        continue;
                                    case 2628:
                                        errorMsgs.Add(InsertError.GenerateErrorMsg(InsertError.E002_STRING_TOOLONG, rowCount));
                                        continue;
                                    case 2627:
                                        errorMsgs.Add(InsertError.GenerateErrorMsg(InsertError.E003_ID_DUPLICATE, rowCount));
                                        continue;
                                    case 109:
                                        errorMsgs.Add(InsertError.GenerateErrorMsg(InsertError.E007_NOT_ENOUGH_VALUE, rowCount));
                                        continue;
                                    default:
                                        errorMsgs.Add(InsertError.GenerateErrorMsg(InsertError.E1000_UNEXPECTED_ERROR, rowCount));
                                        continue;
                                }
                            }
                            catch (Exception e)
                            {
                                transaction.Rollback();
                                Debug.WriteLine(e.ToString());

                                errorMsgs.Add(InsertError.GenerateErrorMsg(InsertError.E1000_UNEXPECTED_ERROR, rowCount));
                                continue;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());

                        errorMsgs.Add(InsertError.GenerateErrorMsg(InsertError.E1000_UNEXPECTED_ERROR, rowCount));
                        continue;
                    }

                }
            }

            CsvInsertResult insertResult = new CsvInsertResult();
            insertResult.errorMsgs = errorMsgs;
            insertResult.result = rowCount + "行 エラー: " + errorMsgs.Count + "件";

            JavaScriptSerializer js = new JavaScriptSerializer();

            //StringBuilder errorMsgSb = new StringBuilder();
            //foreach(string msg in errorMsgs)
            //{
            //    errorMsgSb.Append(msg);
            //}

            //string errorMsg = errorMsgSb.ToString();

            //Debug.WriteLine(errorMsg);

            // Listをjsonの形にする
            string json = js.Serialize(insertResult);

            // jsonを返す
            return json;
        }

        [System.Web.Services.WebMethod]
        public static void DeleteButton_Click(string idStr)
        {
            int id = Convert.ToInt32(idStr);

            string connectionString = ConfigurationManager.ConnectionStrings["MyConnectionString"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    SqlCommand command = new SqlCommand();

                    string query = @"UPDATE M_Customer SET delete_flag = 1 WHERE id = @id";

                    command.Parameters.Add(new SqlParameter("@id", id));

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
                        }
                        catch (Exception e)
                        {
                            transaction.Rollback();
                            Debug.WriteLine(e.ToString());
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }

            }

        }

        [System.Web.Services.WebMethod]
        public static string SortTable(List<List<string>> tableData, string columnName, string sortMethod)
        {
            Debug.WriteLine(columnName);

            // クリックされたカラムがidなら、idを基準に昇順、降順でソートする
            if(columnName == "id")
            {
                if (sortMethod == "asc")
                {
                    tableData.Sort((a, b) => Convert.ToInt32(a[0]).CompareTo(Convert.ToInt32(b[0])));
                } else if(sortMethod == "desc")
                {
                    tableData.Sort((a, b) => Convert.ToInt32(b[0]).CompareTo(Convert.ToInt32(a[0])));
                }
            }

            // クリックされたカラムが誕生日なら、誕生日を基準に昇順、降順でソートする
            if (columnName == "birthday")
            {
                if (sortMethod == "asc")
                {
                    tableData.Sort((a, b) => DateTime.Parse(a[4]).CompareTo(DateTime.Parse(b[4])));
                } else if(sortMethod == "desc")
                {
                    tableData.Sort((a, b) => DateTime.Parse(b[4]).CompareTo(DateTime.Parse(a[4])));
                }
            }

            JavaScriptSerializer js = new JavaScriptSerializer();

            // Listをjsonの形にする
            string json = js.Serialize(tableData);

            Debug.WriteLine(json);

            // jsonを返す
            return json;
        }
    }
}