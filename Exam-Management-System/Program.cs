using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

string connectionString = "Server=localhost;Database=ExamManagement;User ID=root;Password='';";

while (true)
{
    Console.WriteLine("Choose an option:");
    Console.WriteLine("1. Check GPA Results");
    Console.WriteLine("2. Enter Module Grades and Calculate GPA");
    Console.WriteLine("3. Exit");

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
    string choice = Console.ReadLine();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

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

void CheckGPAResults()
{
    using (var connection = new MySqlConnection(connectionString))
    {
        connection.Open();

        // Retrieve students' GPA results
        var students = GetStudents(connection);

        foreach (var student in students)
        {
            var query = "SELECT * FROM GPA WHERE StudentID = @StudentID";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@StudentID", student.StudentID);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine($"Student ID: {reader["StudentID"]}, Name: {reader["Name"]}, Degree: {reader["Degree"]}, Semester GPA: {reader["SemesterGPA"]}, Overall GPA: {reader["OverallGPA"]}");
                    }
                }
            }
        }
    }
}

void EnterModuleGradesAndCalculateGPA()
{
    using (var connection = new MySqlConnection(connectionString))
    {
        connection.Open();

        Console.WriteLine("Enter Student ID:");
        int studentID = Convert.ToInt32(Console.ReadLine());

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

List<Student> GetStudents(MySqlConnection connection)
{
    var students = new List<Student>();

    var query = "SELECT StudentID, Name, Degree FROM Students";
    using (var command = new MySqlCommand(query, connection))
    {
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
#pragma warning disable CS8601 // Possible null reference assignment.
            var student = new Student
            {
                StudentID = Convert.ToInt32(reader["StudentID"]),
                Name = Convert.ToString(reader["Name"]),
                Degree = Convert.ToString(reader["Degree"])
            };
#pragma warning restore CS8601 // Possible null reference assignment.
            students.Add(student);
        }
    }

    return students;
}

List<Module> GetModulesByStudent(MySqlConnection connection, int studentID)
{
    var modules = new List<Module>();

    var query = "SELECT ModuleName, Credits FROM Modules WHERE StudentID = @StudentID";
    using (var command = new MySqlCommand(query, connection))
    {
        command.Parameters.AddWithValue("@StudentID", studentID);

        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
#pragma warning disable CS8601 // Possible null reference assignment.
                var module = new Module
                {
                    ModuleName = Convert.ToString(reader["ModuleName"]),
                    Credits = Convert.ToInt32(reader["Credits"]),
                    Grade = Convert.ToDouble(reader["Grade"]) // Assuming Grade is stored as double in Modules table
                };
#pragma warning restore CS8601 // Possible null reference assignment.
                modules.Add(module);
            }
        }
    }

    return modules;
}

float CalculateSemesterGPA(List<Module> modules)
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


float CalculateOverallGPA(MySqlConnection connection, int studentID)
{
    float overallGPA = 0;
    int semesterCount = 0;

    var query = "SELECT SemesterGPA FROM GPA WHERE StudentID = @StudentID";
    using (var command = new MySqlCommand(query, connection))
    {
        command.Parameters.AddWithValue("@StudentID", studentID);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            overallGPA += Convert.ToSingle(reader["SemesterGPA"]);
            semesterCount++;
        }
    }

    if (semesterCount > 0)
        return overallGPA / semesterCount;
    else
        return 0.0f;
}

void UpdateGPA(MySqlConnection connection, int studentID, float semesterGPA, float overallGPA)
{
    var query = "INSERT INTO GPA (StudentID, SemesterGPA, OverallGPA) VALUES (@StudentID, @SemesterGPA, @OverallGPA)";
    using (var command = new MySqlCommand(query, connection))
    {
        command.Parameters.AddWithValue("@StudentID", studentID);
        command.Parameters.AddWithValue("@SemesterGPA", semesterGPA);
        command.Parameters.AddWithValue("@OverallGPA", overallGPA);
        command.ExecuteNonQuery();
    }
}

class Student
{
    public int StudentID { get; set; }
    public required string Name { get; set; }
    public required string Degree { get; set; }
}

class Module
{
    public required string ModuleName { get; set; }
    public int Credits { get; set; }
    public double Grade { get; set; }
}
