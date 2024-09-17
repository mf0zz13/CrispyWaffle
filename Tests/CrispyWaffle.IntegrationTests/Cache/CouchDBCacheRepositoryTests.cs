using System;
using System.Threading.Tasks;
using CrispyWaffle.Configuration;
using CrispyWaffle.CouchDB.Cache;
using CrispyWaffle.CouchDB.Utils.Communications;
using Xunit;

namespace CrispyWaffle.IntegrationTests.Cache;

public class CouchDBCacheRepositoryTests : IDisposable
{
    private readonly CouchDBCacheRepository _repository;

    public CouchDBCacheRepositoryTests()
    {
        var connection = new Connection
        {
            Host = "http://localhost",
            Port = 5984,
            Credentials = { Username = "Admin", Password = "myP@ssw0rd" },
        };
        var connector = new CouchDBConnector(connection);
        _repository = new CouchDBCacheRepository(connector);
    }

    /// <summary>
    /// Tests the Get and Set functionality of the CouchDB repository.
    /// </summary>
    /// <remarks>
    /// This test method verifies that a CouchDBCacheDocument can be successfully set in the repository and then retrieved.
    /// It creates a new instance of CouchDBCacheDocument, sets it in the repository with a unique key,
    /// and retrieves it back to ensure that the key matches the original document's key.
    /// Finally, it cleans up by removing the document from the repository.
    /// This ensures that the repository's Get and Set methods are functioning correctly.
    /// </remarks>
    /// <exception cref="Exception">Throws an exception if the document retrieval fails or if the keys do not match.</exception>
    [Fact]
    public async Task GetAndSetCouchDocTestAsync()
    {
        var doc = new CouchDBCacheDocument();

        await _repository.SetAsync(doc, Guid.NewGuid().ToString());

        var docDB = await _repository.GetAsync<CouchDBCacheDocument>(doc.Key);

        Assert.True(doc.Key == docDB.Key);

        await _repository.RemoveAsync(doc.Key);
    }

    /// <summary>
    /// Tests the Get and Set functionality for specific Car objects in the repository.
    /// </summary>
    /// <remarks>
    /// This unit test verifies that the repository can correctly set and retrieve specific Car objects.
    /// It creates two Car instances with different makers, sets them in the repository with unique keys,
    /// and then retrieves them to ensure that the data matches the expected values.
    /// The test checks that the keys and makers of the retrieved Car objects are as expected.
    /// Finally, it cleans up by removing the Car objects from the repository after the assertions.
    /// </remarks>
    [Fact]
    public async Task GetAndSetSpecificTestAsync()
    {
        var docOne = new Car("MakerOne");

        await _repository.SetSpecificAsync(
            docOne,
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString()
        );

        var docTwo = new Car("MakerTwo");

        await _repository.SetSpecificAsync(docTwo, Guid.NewGuid().ToString());

        var docDB = await _repository.GetSpecificAsync<Car>(docOne.Key);

        Assert.True(
            docOne.Key == docDB.Key && docOne.SubKey == docDB.SubKey && docOne.Maker == "MakerOne"
        );

        docDB = await _repository.GetSpecificAsync<Car>(docTwo.Key);

        Assert.True(docTwo.Key == docDB.Key && docTwo.Maker == "MakerTwo");

        await _repository.RemoveSpecificAsync<Car>(docOne.Key);
        await _repository.RemoveSpecificAsync<Car>(docTwo.Key);
    }

    /// <summary>
    /// Tests the removal of a CouchDB document from the repository.
    /// </summary>
    /// <remarks>
    /// This test method creates a new instance of <see cref="CouchDBCacheDocument"/> and sets it in the repository with a unique key generated using <see cref="Guid.NewGuid"/>.
    /// After setting the document, it removes the document using the same key.
    /// Finally, it retrieves the document from the repository to verify that it has been successfully removed by asserting that the retrieved document is equal to the default value for <see cref="CouchDBCacheDocument"/>.
    /// This ensures that the removal functionality of the repository works as expected.
    /// </remarks>
    [Fact]
    public async Task RemoveCouchDocTestAsync()
    {
        var doc = new CouchDBCacheDocument();

        await _repository.SetAsync(doc, Guid.NewGuid().ToString());

        await _repository.RemoveAsync(doc.Key);

        var docDB = await _repository.GetAsync<CouchDBCacheDocument>(doc.Key);

        Assert.True(docDB == default);
    }

    /// <summary>
    /// Tests the removal of a specific document from the repository.
    /// </summary>
    /// <remarks>
    /// This test method creates a new instance of a <see cref="Car"/> object with a specified maker.
    /// It then sets this object in the repository using a unique identifier generated by <see cref="Guid.NewGuid"/>.
    /// After that, it removes the specific document from the repository using the key of the created <see cref="Car"/> object.
    /// Finally, it retrieves the document from the repository to verify that it has been successfully removed.
    /// The assertion checks that the retrieved document is equal to the default value, indicating that the document no longer exists in the repository.
    /// </remarks>
    [Fact]
    public async Task RemoveSpecificTestAsync()
    {
        var doc = new Car("Maker");

        await _repository.SetSpecificAsync(doc, Guid.NewGuid().ToString());

        await _repository.RemoveSpecificAsync<Car>(doc.Key);

        var docDB = await _repository.GetAsync<CouchDBCacheDocument>(doc.Key);

        Assert.True(docDB == default);
    }

    /// <summary>
    /// Tests the functionality of clearing the database repository.
    /// </summary>
    /// <remarks>
    /// This test method first populates the repository with several instances of <see cref="CouchDBCacheDocument"/>
    /// by calling the <see cref="_repository.Set"/> method multiple times. Each call assigns a new document
    /// with a unique identifier generated by <see cref="Guid.NewGuid"/>. After adding the documents,
    /// the <see cref="_repository.Clear"/> method is invoked to remove all documents from the repository.
    /// Finally, the test checks that the document count in the repository is zero using the
    /// <see cref="_repository.GetDocCount{T}"/> method, ensuring that the clear operation was successful.
    /// This method is marked with the <see cref="[Fact]"/> attribute, indicating that it is a unit test.
    /// </remarks>
    [Fact]
    public async Task DatabaseClearTestAsync()
    {
        await _repository.SetAsync(new CouchDBCacheDocument(), Guid.NewGuid().ToString());
        await _repository.SetAsync(new CouchDBCacheDocument(), Guid.NewGuid().ToString());
        await _repository.SetAsync(new CouchDBCacheDocument(), Guid.NewGuid().ToString());
        await _repository.SetAsync(new CouchDBCacheDocument(), Guid.NewGuid().ToString());

        await _repository.ClearAsync();

        var count = _repository.GetDocCount<CouchDBCacheDocument>();

        Assert.True(count == 0);
    }

    /// <summary>
    /// Tests the Time-To-Live (TTL) functionality of the CouchDB cache.
    /// </summary>
    /// <remarks>
    /// This asynchronous test method verifies that a document stored in the CouchDB cache
    /// expires after a specified duration. It first creates a new CouchDBCacheDocument with
    /// a unique key and sets it in the repository with a TTL of 5 seconds. The test then
    /// retrieves the document from the repository and asserts that the retrieved document's
    /// key matches the original document's key. After waiting for 6 seconds, which exceeds
    /// the TTL, the test attempts to retrieve the document again and asserts that it is no
    /// longer available (i.e., should be null). This ensures that the cache correctly handles
    /// document expiration based on the TTL setting.
    /// </remarks>
    [Fact]
    public async Task TTLGetTestAsync()
    {
        var doc = new CouchDBCacheDocument() { Key = Guid.NewGuid().ToString() };

        await _repository.SetAsync(new CouchDBCacheDocument(), doc.Key, new TimeSpan(0, 0, 5));
        var fromDB = await _repository.GetAsync<CouchDBCacheDocument>(doc.Key);

        Assert.True(doc.Key == fromDB.Key);

        await Task.Delay(6000);

        fromDB = await _repository.GetAsync<CouchDBCacheDocument>(doc.Key);

        Assert.True(fromDB == null);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the resources used by the current instance of the class.
    /// </summary>
    /// <param name="disposing">A boolean value indicating whether the method was called directly or indirectly by a user's code.</param>
    /// <remarks>
    /// This method is part of the IDisposable pattern. When disposing is true, the method releases all managed resources,
    /// such as the repository instance, by calling its Dispose method. If disposing is false, the method does not
    /// release managed resources, as it is being called by the finalizer. This ensures that resources are properly
    /// cleaned up when the object is no longer needed, preventing memory leaks and ensuring efficient resource management.
    /// </remarks>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _repository?.Dispose();
        }
    }
}

public class Car : CouchDBCacheDocument
{
    public Car(string maker)
    {
        Maker = maker;
    }

    public string Maker { get; set; }
}
