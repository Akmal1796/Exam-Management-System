using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

class Program
{
    static string connectionString = "Server=localhost;Database=ExamManagement;Uid=root;Pwd='';";

    static void Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("Choose an option:");
            Console.WriteLine("1. Check GPA Results");
            Console.WriteLine("2. Enter Module Grades and Calculate GPA");
            Console.WriteLine("3. Exit");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    CheckGPAResults();
                    break;
                case "2":
                    EnterModuleGradesAndCalculateGPA();
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }
        }
    }

    static void CheckGPAResults()
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            Console.WriteLine("Enter Student ID to check GPA:");
            int studentID = Convert.ToInt32(Console.ReadLine());

            if (!StudentExists(connection, studentID))
            {
                Console.WriteLine("Student ID does not exist.");
                return;
            }

            var query = "SELECT * FROM GPA WHERE StudentID = @StudentID";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@StudentID", studentID);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine($"Student ID: {reader["StudentID"]}, Name: {reader["Name"]}, Degree: {reader["Degree"]}, Semester GPA: {reader["SemesterGPA"]}, Overall GPA: {reader["OverallGPA"]}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No GPA records found for this student.");
                    }
                }
            }
        }
    }

    static void EnterModuleGradesAndCalculateGPA()
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            Console.WriteLine("Enter Student ID:");
            int studentID = Convert.ToInt32(Console.ReadLine());

            if (!StudentExists(connection, studentID))
            {
                Console.WriteLine("Student ID does not exist.");
                return;
            }

            // Retrieve modules taken by the student
            var modules = GetModulesByStudent(connection, studentID);

            // Calculate semester GPA
            float semesterGPA = CalculateSemesterGPA(modules);

            // Calculate overall GPA
            float overallGPA = CalculateOverallGPA(connection, studentID);

            // Update GPA table
            UpdateGPA(connection, studentID, semesterGPA, overallGPA);

            Console.WriteLine($"Updated GPA for Student ID {studentID}: Semester GPA = {semesterGPA}, Overall GPA = {overallGPA}");
        }
    }

    static bool StudentExists(MySqlConnection connection, int studentID)
    {
        var query = "SELECT COUNT(*) FROM Students WHERE StudentID = @StudentID";
        using (var command = new MySqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@StudentID", studentID);
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }
    }

    static List<Module> GetModulesByStudent(MySqlConnection connection, int studentID)
    {
        var modules = new List<Module>();

        var query = "SELECT ModuleName, Credits, Grade FROM Modules WHERE StudentID = @StudentID";
        using (var command = new MySqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@StudentID", studentID);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var module = new Module
                    {
                        ModuleName = Convert.ToString(reader["ModuleName"]),
                        Credits = Convert.ToInt32(reader["Credits"]),
                        Grade = Convert.ToDouble(reader["Grade"]) // Ensure Grade is retrieved correctly
                    };
                    modules.Add(module);
                }
            }
        }

        return modules;
    }

    static float CalculateSemesterGPA(List<Module> modules)
    {
        if (modules.Count == 0)
            return 0.0f;

        float totalCredits = 0;
        float totalGradePoints = 0;

        foreach (var module in modules)
        {
            totalCredits += module.Credits;
            totalGradePoints += (float)module.Grade * module.Credits; // Explicitly cast module.Grade to float
        }

        return totalGradePoints / totalCredits;
    }

    static float CalculateOverallGPA(MySqlConnection connection, int studentID)
    {
        float overallGPA = 0;
        int semesterCount = 0;

        var query = "SELECT SemesterGPA FROM GPA WHERE StudentID = @StudentID";
        using (var command = new MySqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@StudentID", studentID);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    overallGPA += Convert.ToSingle(reader["SemesterGPA"]);
                    semesterCount++;
                }
            }
        }

        if (semesterCount > 0)
            return overallGPA / semesterCount;
        else
            return 0.0f;
    }

    static void UpdateGPA(MySqlConnection connection, int studentID, float semesterGPA, float overallGPA)
    {
        var query = "INSERT INTO GPA (StudentID, Name, Degree, SemesterGPA, OverallGPA) " +
                    "VALUES (@StudentID, (SELECT Name FROM Students WHERE StudentID = @StudentID), " +
                    "(SELECT Degree FROM Students WHERE StudentID = @StudentID), @SemesterGPA, @OverallGPA)";
        using (var command = new MySqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@StudentID", studentID);
            command.Parameters.AddWithValue("@SemesterGPA", semesterGPA);
            command.Parameters.AddWithValue("@OverallGPA", overallGPA);
            command.ExecuteNonQuery();
        }
    }
}

class Module
{
    public string ModuleName { get; set; }
    public int Credits { get; set; }
    public double Grade { get; set; }
}
