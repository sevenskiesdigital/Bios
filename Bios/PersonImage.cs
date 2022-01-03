using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System.Diagnostics;
namespace Bios
{

    public class PersonImage
    {
        public string? ImageUrl { get; set; }
        public string? ImageBase64 { get; set; }

        public async Task addPersonImage()
        {
            try
            {
                string temp = @ImageUrl.Replace(Constants.imagesDirectory + Path.DirectorySeparatorChar, "");
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
            } catch
            {
            }
            
        }
        void PrintException(Exception? ex)
        {
            if (ex != null)
            {
                Debug.WriteLine($"Message: {ex.Message}");
                Debug.WriteLine("Stacktrace:");
                Debug.WriteLine(ex.StackTrace);
                PrintException(ex.InnerException);
            }
        }
    }

   
}
