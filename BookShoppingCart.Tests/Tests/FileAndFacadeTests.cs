using BookShoppingCart.Business.Facades;
using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using BookStoreCore.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
namespace BookShoppingCart.Tests.Tests;
public class FileAndFacadeTests
{
    [Theory] [InlineData("../outside.txt")] [InlineData("..\\outside.txt")] [InlineData("C:/outside.txt")]
    public void FileDeletion_RejectsTraversal(string name)
    {
        var root = Path.Combine(Path.GetTempPath(), "bookstore-file-tests-"+Guid.NewGuid());
        Directory.CreateDirectory(root);
        try
        {
            var env = new Mock<IWebHostEnvironment>(); env.SetupGet(e=>e.WebRootPath).Returns(root);
            var service = new FileService(env.Object, NullLogger<FileService>.Instance);
            Assert.Throws<ArgumentException>(()=>service.DeleteFile(name));
        }
        finally { Directory.Delete(root,true); }
    }
    [Fact] public async Task InvalidImageContent_IsRejected()
    {
        var root = Path.Combine(Path.GetTempPath(), "bookstore-file-tests-"+Guid.NewGuid());
        Directory.CreateDirectory(root);
        try
        {
            var env = new Mock<IWebHostEnvironment>(); env.SetupGet(e=>e.WebRootPath).Returns(root);
            var service = new FileService(env.Object, NullLogger<FileService>.Instance);
            await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("not an image"));
            var file = new FormFile(stream,0,stream.Length,"ImageFile","cover.png");
            await Assert.ThrowsAsync<ArgumentException>(()=>service.SaveFile(file,[".png"]));
            Assert.Empty(Directory.GetFiles(Path.Combine(root,"images")));
        }
        finally { Directory.Delete(root,true); }
    }
    [Fact] public async Task Facade_DiscardsPostedImageName_AndCleansNewUploadOnSaveFailure()
    {
        var books = new Mock<IBookService>();
        books.Setup(b=>b.AddBook(It.IsAny<Book>())).ThrowsAsync(new ArgumentException("invalid price"));
        var files = new Mock<IFileService>();
        files.Setup(f=>f.SaveFile(It.IsAny<IFormFile>(),It.IsAny<IEnumerable<string>>())).ReturnsAsync("generated.png");
        var facade = new BookFacade(books.Object,files.Object);
        var dto = new BookDTO { Image="../forged.txt", ImageFile=Mock.Of<IFormFile>() };
        await Assert.ThrowsAsync<ArgumentException>(()=>facade.AddBook(dto));
        books.Verify(b=>b.AddBook(It.Is<Book>(b=>b.Image=="generated.png")), Times.Once);
        files.Verify(f=>f.DeleteFile("generated.png"),Times.Once);
        files.Verify(f=>f.DeleteFile("../forged.txt"),Times.Never);
    }
    [Fact] public async Task UnconfirmedRemoteSave_DoesNotDeletePotentiallyReferencedCover()
    {
        var books = new Mock<IBookService>();
        books.Setup(b => b.AddBook(It.IsAny<Book>())).ThrowsAsync(new BookWriteUnconfirmedException(new HttpRequestException()));
        var files = new Mock<IFileService>();
        files.Setup(f => f.SaveFile(It.IsAny<IFormFile>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync("generated.png");
        await Assert.ThrowsAsync<BookWriteUnconfirmedException>(() => new BookFacade(books.Object, files.Object)
            .AddBook(new BookDTO { ImageFile = Mock.Of<IFormFile>() }));
        files.Verify(f => f.DeleteFile(It.IsAny<string>()), Times.Never);
    }
    [Fact] public async Task Update_UsesPersistedImageOwnership()
    {
        var books=new Mock<IBookService>(); var files=new Mock<IFileService>();
        books.Setup(b=>b.GetBookById(1)).ReturnsAsync(new Book { Id=1,Image="persisted.png" });
        await new BookFacade(books.Object,files.Object).UpdateBook(new BookDTO { Id=1,Image="../forged.txt" });
        books.Verify(b=>b.UpdateBook(It.Is<Book>(b=>b.Image=="persisted.png")),Times.Once);
        files.Verify(f=>f.DeleteFile(It.IsAny<string>()),Times.Never);
    }
    [Fact] public async Task Clone_LongTitle_IsBounded_AndReferencesSharedAssetSafely()
    {
        var books=new Mock<IBookService>();
        books.Setup(b=>b.GetBookById(1)).ReturnsAsync(new Book { Id=1,BookName=new string('x',40),Image="shared.png" });
        await new BookFacade(books.Object,Mock.Of<IFileService>()).CloneBook(1);
        books.Verify(b=>b.AddBook(It.Is<Book>(b=>b.Id==0 && b.BookName.Length==40 && b.Image=="shared.png")),Times.Once);
    }
}
