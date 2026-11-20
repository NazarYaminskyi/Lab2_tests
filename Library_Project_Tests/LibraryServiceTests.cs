using Library_Project.Model;
using Library_Project.Services;
using Library_Project.Services.Interfaces;
using Moq;

namespace Library_Project_Tests
{
    public class LibraryServiceTests
    {
        private readonly Mock<IBookRepository> _repoMock;
        private readonly Mock<IMemberService> _memberMock;
        private readonly Mock<INotificationService> _notifMock;
        private readonly LibraryService _service;

        public LibraryServiceTests()
        {
            _repoMock = new Mock<IBookRepository>();
            _memberMock = new Mock<IMemberService>();
            _notifMock = new Mock<INotificationService>();

            _service = new LibraryService(_repoMock.Object, _memberMock.Object, _notifMock.Object);
        }

        /// <summary>
        /// Перевіряє, що AddBook викидає ArgumentException, якщо назва книги порожня або складається з пробілів.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void AddBook_InvalidTitle_ThrowsArgumentException(string? invalidTitle)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _service.AddBook(invalidTitle!, 5));
            Assert.Equal("Title required.", exception.Message);
            _repoMock.Verify(r => r.SaveBook(It.IsAny<Book>()), Times.Never);
        }

        /// <summary>
        /// Перевіряє, що GetAvailableBooks повертає лише книги з доступними примірниками (Copies > 0).
        /// </summary>
        [Fact]
        public void GetAvailableBooks_MixedCopies_ReturnsOnlyAvailableBooks()
        {
            // Arrange
            var availableBook = new Book { Title = "Neuromancer", Copies = 2 };
            var unavailableBook = new Book { Title = "Solaris", Copies = 0 };

            _repoMock.Setup(r => r.GetAllBooks()).Returns(new List<Book> { availableBook, unavailableBook });

            // Act
            var result = _service.GetAvailableBooks();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains(availableBook, result);
            Assert.DoesNotContain(unavailableBook, result);
        }

        /// <summary>
        /// Перевіряє, що BorrowBook повертає true, зменшує кількість примірників та викликає збереження і сповіщення при валідних даних.
        /// </summary>
        [Fact]
        public void BorrowBook_ValidMemberAndAvailableBook_ReturnsTrueAndUpdatesCopies()
        {
            // Arrange
            int memberId = 10;
            string title = "Dune";
            var book = new Book { Title = title, Copies = 3 };

            _memberMock.Setup(m => m.IsValidMember(memberId)).Returns(true);
            _repoMock.Setup(r => r.FindBook(title)).Returns(book);

            // Act
            var result = _service.BorrowBook(memberId, title);

            // Assert
            Assert.True(result);
            Assert.Equal(2, book.Copies);
            _repoMock.Verify(r => r.SaveBook(It.Is<Book>(b => b.Title == title && b.Copies == 2)), Times.Exactly(1));
            _notifMock.Verify(n => n.NotifyBorrow(memberId, title));
        }

        /// <summary>
        /// Перевіряє, що ReturnBook повертає false, якщо книгу не знайдено в репозиторії.
        /// </summary>
        [Fact]
        public void ReturnBook_BookNotFound_ReturnsFalse()
        {
            // Arrange
            string title = "Unknown Book";
            _repoMock.Setup(r => r.FindBook(title)).Returns((Book?)null);

            // Act
            var result = _service.ReturnBook(5, title);

            // Assert
            Assert.False(result);
            _notifMock.Verify(n => n.NotifyReturn(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }
    }
}
