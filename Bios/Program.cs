using Microsoft.EntityFrameworkCore;
using Bios;

using System.Diagnostics;

using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<UserContext>(o => o.UseInMemoryDatabase(databaseName: "InMemoryDb"));
builder.Services.AddDbContext<UserImageContext>(o => o.UseInMemoryDatabase(databaseName: "InMemoryDb"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

//our custom middleware extension to call UseMiddleware
app.UseAPIKeyCheckMiddleware();

app.MapGet("/", () => "Hello World!");
// app.MapGet("/users", async (UserContext db) => await db.Users.ToListAsync());
// app.MapGet("/users/{id}", async (UserContext db, int id) => await db.Users.FindAsync(id));
app.MapGet("/users", 
    async (UserContext db, string? EmployeeNumber, string? PersonGroupId) => 
    await db.Users.Where(x => (EmployeeNumber == null || x.EmployeeNumber == EmployeeNumber)
                          && (PersonGroupId == null || x.PersonGroupId == PersonGroupId))
                          .ToListAsync());
app.MapPost("/users", (UserContext db, User user) =>
{
    db.Users.Add(user);
    db.SaveChanges();
    Results.Accepted();
});

app.MapGet("/userImages/{id}", async (UserImageContext db, int id) => await db.UserImages.FindAsync(id));
app.MapGet("/userImages",
    async (UserImageContext db, int? userId) =>
    await db.UserImages.Where(x => (userId == null || x.UserId == userId))
                          .ToListAsync());
app.MapPost("/userImages", (UserImageContext db, UserImage userImage) =>
{
    db.UserImages.Add(userImage);
    db.SaveChanges();
    Results.Accepted();
});

app.MapPost("/vision", async (imageUrl imageUrl) =>
{
    string CogServicesSecret = "9ecb457394bf4052af128281000652a8";
    string Endpoint = "https://bios.cognitiveservices.azure.com/";
    var credentials = new ApiKeyServiceClientCreds(CogServicesSecret);
    var client = new ComputerVisionClient(credentials)
    {
        Endpoint = Endpoint
    };

    DetectResult analysis = await client.DetectObjectsAsync(imageUrl.Url);
    Debug.WriteLine(imageUrl.Url);
    Debug.WriteLine(analysis.Objects.Count);
    foreach (var obj in analysis.Objects)
    {
        Debug.WriteLine($"{obj.ObjectProperty} with confidence {obj.Confidence}");
    }
    //Results.Ok(analysis.Objects[0]);
    return analysis.Objects;
});
app.MapPost("/face", async (imageUrl imageUrl) =>
{
    string CogServicesSecret = "9ecb457394bf4052af128281000652a8";
    string Endpoint = "https://bios.cognitiveservices.azure.com/";
    var credentials = new ApiKeyServiceClientCreds(CogServicesSecret);
    var client = new FaceClient(credentials)
    {
        Endpoint = Endpoint
    };

    IList<DetectedFace> faceList = await client.Face.DetectWithUrlAsync(imageUrl.Url);
    Debug.WriteLine(imageUrl.Url);
    Debug.WriteLine(faceList.Count);
    //Results.Ok(faceList);
    return faceList;
});

app.MapPost("/face/identification", async (imageUrl imageUrl) =>
{
    string CogServicesSecret = "9ecb457394bf4052af128281000652a8";
    string Endpoint = "https://bios.cognitiveservices.azure.com/";
    var credentials = new ApiKeyServiceClientCreds(CogServicesSecret);
    var client = new FaceClient(credentials)
    {
        Endpoint = Endpoint
    };    

    try
    {
        List<Guid> sourceFaceIds = new List<Guid>();
        // Detect faces from source image url.
        Debug.WriteLine(imageUrl.Url);
        Debug.WriteLine(imageUrl.PersonGroupId);
        IList<DetectedFace> detectedFaces = await client.Face.DetectWithUrlAsync(imageUrl.Url);
        Debug.WriteLine(detectedFaces.Count);

        // Add detected faceId to sourceFaceIds.
        foreach (var detectedFace in detectedFaces) 
        { 
            sourceFaceIds.Add(detectedFace.FaceId.Value);
            Debug.WriteLine(detectedFace.FaceId.Value);
        }
        Debug.WriteLine(sourceFaceIds.Count);

        var identifyResults = await client.Face.IdentifyAsync(faceIds: sourceFaceIds, largePersonGroupId: imageUrl.PersonGroupId);
        Debug.WriteLine(identifyResults.Count);

        Debug.WriteLine(identifyResults[0].Candidates[0].PersonId.ToString());
        var userContextOptions = new DbContextOptionsBuilder<UserContext>()
            .UseInMemoryDatabase(databaseName: "InMemoryDb")
            .Options;
        UserContext dbUser = new UserContext(userContextOptions);
        var users = dbUser.Users.Where(x => x.PersonId == identifyResults[0].Candidates[0].PersonId.ToString()   
                              && x.PersonGroupId == imageUrl.PersonGroupId)
                              .ToList();
        //Results.Ok(faceList);
        return users;
    }
    catch (Exception e)
    {
        PrintException(e);
        return null;
    }

});

app.MapPost("/face/verification", async (imageUrl imageUrl) =>
{
    string CogServicesSecret = "9ecb457394bf4052af128281000652a8";
    string Endpoint = "https://bios.cognitiveservices.azure.com/";
    var credentials = new ApiKeyServiceClientCreds(CogServicesSecret);
    var client = new FaceClient(credentials)
    {
        Endpoint = Endpoint
    };

    try
    {
        List<Guid> sourceFaceIds = new List<Guid>();
        // Detect faces from source image url.
        Debug.WriteLine(imageUrl.Url);
        Debug.WriteLine(imageUrl.PersonGroupId);
        IList<DetectedFace> detectedFaces = await client.Face.DetectWithUrlAsync(imageUrl.Url);
        Debug.WriteLine(detectedFaces.Count);

        // Add detected faceId to sourceFaceIds.
        foreach (var detectedFace in detectedFaces)
        {
            sourceFaceIds.Add(detectedFace.FaceId.Value);
            Debug.WriteLine(detectedFace.FaceId.Value);
        }
        Debug.WriteLine(sourceFaceIds.Count);

        if (imageUrl.EmployeeNumber != null)
        {
            var userContextOptions = new DbContextOptionsBuilder<UserContext>()
                .UseInMemoryDatabase(databaseName: "InMemoryDb")
                .Options;
            UserContext dbUser = new UserContext(userContextOptions);
            var users = dbUser.Users.Where(x => x.EmployeeNumber == imageUrl.EmployeeNumber
                                  && x.PersonGroupId == imageUrl.PersonGroupId)
                                  .ToList();
            int m = users.Count;
            if (m>0)
            {
                imageUrl.PersonId = users[0].PersonId;
            }
        }

        var verifyResults = await client.Face.VerifyFaceToPersonAsync(faceId: sourceFaceIds[0], personId: new Guid(imageUrl.PersonId), largePersonGroupId: imageUrl.PersonGroupId);
        
        return verifyResults;
    }
    catch (Exception e)
    {
        PrintException(e);
        return null;
    }

});

app.MapPost("/personGroup/training", async (PersonGroup PG) =>
{
    string CogServicesSecret = "9ecb457394bf4052af128281000652a8";
    string Endpoint = "https://bios.cognitiveservices.azure.com/";
    var credentials = new ApiKeyServiceClientCreds(CogServicesSecret);
    var client = new FaceClient(credentials)
    {
        Endpoint = Endpoint
    };

    await client.LargePersonGroup.TrainAsync(PG.PersonGroupId);
    Debug.WriteLine(PG.PersonGroupId);

    // Wait until the training is completed.
    while (true)
    {
        await Task.Delay(1000);
        var trainingStatus = await client.LargePersonGroup.GetTrainingStatusAsync(PG.PersonGroupId);
        Debug.WriteLine($"Training status: {trainingStatus.Status}.");
        if (trainingStatus.Status == TrainingStatusType.Succeeded) { break; }
    }

    return Results.Ok("success");
});

app.MapPost("/personGroup", async (PersonGroup PG) =>
{
    string CogServicesSecret = "9ecb457394bf4052af128281000652a8";
    string Endpoint = "https://bios.cognitiveservices.azure.com/";
    var credentials = new ApiKeyServiceClientCreds(CogServicesSecret);
    var client = new FaceClient(credentials)
    {
        Endpoint = Endpoint
    };

    await client.LargePersonGroup.CreateAsync(PG.PersonGroupId, PG.Name);
    Debug.WriteLine(PG.PersonGroupId);
    Debug.WriteLine(PG.Name);

    return Results.Ok("success");
});

app.MapDelete("/personGroup", async (string personGroupId) =>
{
    string CogServicesSecret = "9ecb457394bf4052af128281000652a8";
    string Endpoint = "https://bios.cognitiveservices.azure.com/";
    var credentials = new ApiKeyServiceClientCreds(CogServicesSecret);
    var client = new FaceClient(credentials)
    {
        Endpoint = Endpoint
    };

    try
    {
        await client.LargePersonGroup.DeleteAsync(personGroupId);
        return Results.Ok("success");
    }
    catch 
    {
        return Results.NotFound();
    }

});

app.MapGet("/personGroups", async (string personGroupId) =>
{
    string CogServicesSecret = "9ecb457394bf4052af128281000652a8";
    string Endpoint = "https://bios.cognitiveservices.azure.com/";
    var credentials = new ApiKeyServiceClientCreds(CogServicesSecret);
    var client = new FaceClient(credentials)
    {
        Endpoint = Endpoint
    };
    try
    {
        var largePersonGroup = await client.LargePersonGroup.GetAsync(personGroupId);
        Debug.WriteLine(largePersonGroup);
        // return largePersonGroup;
        return largePersonGroup;
    } catch (APIErrorException e)
    {
        PrintException(e);
        return null;
    }
});

app.MapGet("/persons", async (string personGroupId, string personId) =>
{
    string CogServicesSecret = "9ecb457394bf4052af128281000652a8";
    string Endpoint = "https://bios.cognitiveservices.azure.com/";
    var credentials = new ApiKeyServiceClientCreds(CogServicesSecret);
    var client = new FaceClient(credentials)
    {
        Endpoint = Endpoint
    };

    try
    {
        var persons = await client.LargePersonGroupPerson.GetAsync(personGroupId, new Guid(personId));
        Debug.WriteLine(persons);
        // return largePersonGroup;
        return persons;
    }
    catch (APIErrorException e)
    {
        PrintException(e);
        return null;
    }
});

app.MapPost("/person", async (personData P) =>
{
    string CogServicesSecret = "9ecb457394bf4052af128281000652a8";
    string Endpoint = "https://bios.cognitiveservices.azure.com/";
    var credentials = new ApiKeyServiceClientCreds(CogServicesSecret);
    var client = new FaceClient(credentials)
    {
        Endpoint = Endpoint
    };

    Person person = await client.LargePersonGroupPerson.CreateAsync(largePersonGroupId: P.personGroupId, name: P.name);
    Debug.WriteLine(person.PersonId);
    Debug.WriteLine(person.Name);

    return Results.Ok(person);
});

app.MapDelete("/person", async (string personGroupId, string personId) =>
{
    string CogServicesSecret = "9ecb457394bf4052af128281000652a8";
    string Endpoint = "https://bios.cognitiveservices.azure.com/";
    var credentials = new ApiKeyServiceClientCreds(CogServicesSecret);
    var client = new FaceClient(credentials)
    {
        Endpoint = Endpoint
    };

    try
    {
        await client.LargePersonGroupPerson.DeleteAsync(personGroupId, new Guid(personId));
        return Results.Ok("success");
    }
    catch
    {
        return Results.NotFound();
    }

});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


//FileWatcher fileWatcher = new FileWatcher();
// Cek Folder Images
using var watcher = new FileSystemWatcher(Constants.imagesDirectory);
watcher.NotifyFilter = NotifyFilters.Attributes
                 | NotifyFilters.CreationTime
                 | NotifyFilters.DirectoryName
                 | NotifyFilters.FileName
                 | NotifyFilters.LastAccess
                 | NotifyFilters.LastWrite
                 | NotifyFilters.Security
                 | NotifyFilters.Size;

watcher.Changed += OnChanged;
watcher.Created += OnCreated;
watcher.Deleted += OnDeleted;
watcher.Renamed += OnRenamed;
watcher.Error += OnError;

watcher.Filter = "*.*";
watcher.IncludeSubdirectories = true;
watcher.EnableRaisingEvents = true;
Debug.WriteLine("There..");

static void OnChanged(object sender, FileSystemEventArgs e)
{
    if (e.ChangeType != WatcherChangeTypes.Changed)
    {
        return;
    }
    Debug.WriteLine($"Changed: {e.FullPath}");
}

static async void OnCreated(object sender, FileSystemEventArgs e)
{
    string value = $"Created: {e.FullPath}";
    Debug.WriteLine(value);

    string[] extensions = { ".jpg", ".jpeg", ".png" };
    // get the file's extension 
    var ext = (Path.GetExtension(e.FullPath) ?? string.Empty).ToLower();

    // filter file types 
    if (extensions.Any(ext.Equals))
    {
        while (Constants.onProcess)
        {
            Thread.Sleep(2000);
        }
        Constants.onProcess = true;

        string temp = e.FullPath.Replace(Constants.imagesDirectory + Path.DirectorySeparatorChar, "");
        var arrTemp = temp.Split(Path.DirectorySeparatorChar);

        if (arrTemp.Length == 3)
        {
            var PersonGroupId = arrTemp[0];
            var EmployeeNumber = arrTemp[1];

            string CogServicesSecret = "9ecb457394bf4052af128281000652a8";
            string Endpoint = "https://bios.cognitiveservices.azure.com/";
            var credentials = new ApiKeyServiceClientCreds(CogServicesSecret);

            var client = new FaceClient(credentials)
            {
                Endpoint = Endpoint
            };
            try
            {
                Debug.WriteLine("Get personGroup");
                var largePersonGroup = await client.LargePersonGroup.GetAsync(PersonGroupId);
                Debug.WriteLine(largePersonGroup);
            }
            catch (APIErrorException exc)
            {
                Debug.WriteLine("Create personGroup");
                PrintException(exc);
                client.LargePersonGroup.CreateAsync(PersonGroupId, PersonGroupId);
            }

            // user
            var userContextOptions = new DbContextOptionsBuilder<UserContext>()
            .UseInMemoryDatabase(databaseName: "InMemoryDb")
            .Options;
            UserContext dbUser = new UserContext(userContextOptions);
            var users = dbUser.Users.Where(x => x.EmployeeNumber == EmployeeNumber
                                  && x.PersonGroupId == PersonGroupId)
                                  .ToList();
            int m = users.Count;
            User user = new User();
            user.PersonGroupId = PersonGroupId;
            user.EmployeeNumber = EmployeeNumber;
            if (m > 0)
            {
                user = users.First<User>();
            }


            try
            {
                Debug.WriteLine("Get person");
                var persons = await client.LargePersonGroupPerson.GetAsync(PersonGroupId, new Guid(user.PersonId));
                Debug.WriteLine(persons);
            }
            catch (APIErrorException exc)
            {
                Debug.WriteLine("Create person");
                PrintException(exc);
                Person person = await client.LargePersonGroupPerson.CreateAsync(largePersonGroupId: PersonGroupId, name: EmployeeNumber);
                Debug.WriteLine(person.PersonId);
                user.PersonId = person.PersonId.ToString();
                Debug.WriteLine("After add person");
                Debug.WriteLine(user.PersonId);
            }
            catch (ArgumentNullException exc)
            {
                Debug.WriteLine("Create person");
                PrintException(exc);
                Person person = await client.LargePersonGroupPerson.CreateAsync(largePersonGroupId: PersonGroupId, name: EmployeeNumber);
                Debug.WriteLine(person.PersonId);
                user.PersonId = person.PersonId.ToString();
                Debug.WriteLine("After add person");
                Debug.WriteLine(user.PersonId);
            }
            if (m <= 0)
            {
                dbUser.Users.Add(user);
                dbUser.SaveChanges();
            }

            // userImage
            var userImageContextOptions = new DbContextOptionsBuilder<UserImageContext>()
            .UseInMemoryDatabase(databaseName: "InMemoryDb")
            .Options;
            UserImageContext dbUserImage = new UserImageContext(userImageContextOptions);
            int n = dbUserImage.UserImages.Where(x => x.ImageUrl == temp
                                  && x.UserId == user.Id)
                                  .ToList().Count;
            if (n <= 0)
            {
                FileStream fs = new FileStream(Constants.imagesDirectory + Path.DirectorySeparatorChar + temp, FileMode.Open);
                PersistedFace PF = await client.LargePersonGroupPerson.AddFaceFromStreamAsync(largePersonGroupId: PersonGroupId, new Guid(user.PersonId), image: fs);
                Debug.WriteLine(PF.PersistedFaceId);
                var pfId = PF.PersistedFaceId.ToString() == null ? null : PF.PersistedFaceId.ToString();

                Debug.WriteLine(pfId);
                UserImage userImage = new UserImage
                {
                    FaceId = pfId,
                    UserId = user.Id,
                    ImageUrl = temp
                };
                dbUserImage.UserImages.Add(userImage);
                dbUserImage.SaveChanges();
            }
        }

        Constants.onProcess = false;
    }
    

}

static async void OnDeleted(object sender, FileSystemEventArgs e)
{
    string value = $"Deleted: {e.FullPath}";
    Debug.WriteLine(value);

    string[] extensions = { ".jpg", ".jpeg", ".png" };
    // get the file's extension 
    var ext = (Path.GetExtension(e.FullPath) ?? string.Empty).ToLower();

    // filter file types 
    if (extensions.Any(ext.Equals))
    {
        while (Constants.onProcess)
        {
            Thread.Sleep(2000);
        }
        Constants.onProcess = true;

        string temp = e.FullPath.Replace(Constants.imagesDirectory + Path.DirectorySeparatorChar, "");
        var arrTemp = temp.Split(Path.DirectorySeparatorChar);

        if (arrTemp.Length == 3)
        {
            var PersonGroupId = arrTemp[0];
            var EmployeeNumber = arrTemp[1];

            string CogServicesSecret = "9ecb457394bf4052af128281000652a8";
            string Endpoint = "https://bios.cognitiveservices.azure.com/";
            var credentials = new ApiKeyServiceClientCreds(CogServicesSecret);

            var client = new FaceClient(credentials)
            {
                Endpoint = Endpoint
            };

            var userContextOptions = new DbContextOptionsBuilder<UserContext>()
            .UseInMemoryDatabase(databaseName: "InMemoryDb")
            .Options;
            UserContext dbUser = new UserContext(userContextOptions);
            var users = dbUser.Users.Where(x => x.EmployeeNumber == EmployeeNumber
                                  && x.PersonGroupId == PersonGroupId)
                                  .ToList();
            int m = users.Count;

            if (m>0)
            {
                var userImageContextOptions = new DbContextOptionsBuilder<UserImageContext>()
                    .UseInMemoryDatabase(databaseName: "InMemoryDb")
                    .Options;
                UserImageContext dbUserImage = new UserImageContext(userImageContextOptions);
                var userImages = dbUserImage.UserImages.Where(x => x.ImageUrl == temp && x.UserId == users[0].Id)
                                      .ToList();
                int n = userImages.Count;

                if (n > 0)
                {

                    await client.LargePersonGroupPerson.DeleteFaceAsync(largePersonGroupId: PersonGroupId, new Guid(users[0].PersonId),new Guid(userImages[0].FaceId));
                    dbUserImage.UserImages.Remove(userImages[0]);
                    dbUserImage.SaveChanges();
                }
            }
        }
        Constants.onProcess = false;
    }
}


static void OnRenamed(object sender, RenamedEventArgs e)
{
    Debug.WriteLine($"Renamed:");
    Debug.WriteLine($"    Old: {e.OldFullPath}");
    Debug.WriteLine($"    New: {e.FullPath}");
}

static void OnError(object sender, ErrorEventArgs e) =>
    PrintException(e.GetException());

static void PrintException(Exception? ex)
{
    if (ex != null)
    {
        Debug.WriteLine($"Message: {ex.Message}");
        Debug.WriteLine("Stacktrace:");
        Debug.WriteLine(ex.StackTrace);
        PrintException(ex.InnerException);
    }
}

app.Run();


class imageUrl
{
    public string? Url { get; set; }

    public string? PersonGroupId { get; set; }
    public string? PersonId { get; set; }
    public string? EmployeeNumber { get; set; }
}

class personData
{
    public string? personGroupId { get; set; }
    public string? personId { get; set; }
    public string? name { get; set; }
}