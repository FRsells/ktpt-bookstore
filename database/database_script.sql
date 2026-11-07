-- Скрипт базы данных bookstore: структура и начальные данные.
-- Перенос на другой ПК: выполнить целиком в SSMS (подключение к своему экземпляру SQL Server).
IF DB_ID(N'bookstore') IS NULL
    CREATE DATABASE bookstore COLLATE Cyrillic_General_CI_AS;
GO
USE bookstore;
GO
-- Удаление таблиц в обратном порядке зависимостей (для повторного запуска).
DROP TABLE IF EXISTS order_items;
DROP TABLE IF EXISTS orders;
DROP TABLE IF EXISTS book_authors;
DROP TABLE IF EXISTS books;
DROP TABLE IF EXISTS authors;
DROP TABLE IF EXISTS publishers;
DROP TABLE IF EXISTS genres;
DROP TABLE IF EXISTS users;
DROP TABLE IF EXISTS roles;
GO
CREATE TABLE roles (
    role_id   int IDENTITY(1,1) CONSTRAINT pk_roles PRIMARY KEY,
    role_name nvarchar(50) NOT NULL CONSTRAINT uq_roles_name UNIQUE
);
CREATE TABLE users (
    user_id     int IDENTITY(1,1) CONSTRAINT pk_users PRIMARY KEY,
    login       nvarchar(50) NOT NULL CONSTRAINT uq_users_login UNIQUE,
    last_name   nvarchar(60) NOT NULL,
    first_name  nvarchar(60) NOT NULL,
    middle_name nvarchar(60) NULL,
    role_id     int NOT NULL CONSTRAINT fk_users_role REFERENCES roles (role_id)
);
CREATE TABLE genres (
    genre_id   int IDENTITY(1,1) CONSTRAINT pk_genres PRIMARY KEY,
    genre_name nvarchar(100) NOT NULL CONSTRAINT uq_genres_name UNIQUE
);
CREATE TABLE publishers (
    publisher_id   int IDENTITY(1,1) CONSTRAINT pk_publishers PRIMARY KEY,
    publisher_name nvarchar(150) NOT NULL CONSTRAINT uq_publishers_name UNIQUE
);
CREATE TABLE authors (
    author_id   int IDENTITY(1,1) CONSTRAINT pk_authors PRIMARY KEY,
    last_name   nvarchar(60) NOT NULL,
    first_name  nvarchar(60) NOT NULL,
    middle_name nvarchar(60) NULL
);
CREATE TABLE books (
    book_id          int IDENTITY(1,1) CONSTRAINT pk_books PRIMARY KEY,
    isbn             nvarchar(17) NOT NULL CONSTRAINT uq_books_isbn UNIQUE,
    title            nvarchar(200) NOT NULL,
    genre_id         int NOT NULL CONSTRAINT fk_books_genre REFERENCES genres (genre_id),
    publisher_id     int NOT NULL CONSTRAINT fk_books_publisher REFERENCES publishers (publisher_id),
    publication_year smallint NOT NULL,
    description      nvarchar(max) NULL,
    image_path       nvarchar(150) NULL,
    base_price       decimal(10,2) NOT NULL CONSTRAINT chk_books_price CHECK (base_price > 0),
    stock_quantity   int NOT NULL CONSTRAINT df_books_stock DEFAULT (0)
                     CONSTRAINT chk_books_stock CHECK (stock_quantity >= 0)
);
CREATE TABLE book_authors (
    book_id   int NOT NULL CONSTRAINT fk_ba_book REFERENCES books (book_id) ON DELETE CASCADE,
    author_id int NOT NULL CONSTRAINT fk_ba_author REFERENCES authors (author_id),
    CONSTRAINT pk_book_authors PRIMARY KEY (book_id, author_id)
);
CREATE TABLE orders (
    order_id   int IDENTITY(1,1) CONSTRAINT pk_orders PRIMARY KEY,
    order_date date NOT NULL CONSTRAINT df_orders_date DEFAULT (CAST(GETDATE() AS date)),
    client_id  int NOT NULL CONSTRAINT fk_orders_client REFERENCES users (user_id)
);
CREATE TABLE order_items (
    order_item_id int IDENTITY(1,1) CONSTRAINT pk_order_items PRIMARY KEY,
    order_id      int NOT NULL CONSTRAINT fk_items_order REFERENCES orders (order_id) ON DELETE CASCADE,
    book_id       int NOT NULL CONSTRAINT fk_items_book REFERENCES books (book_id),
    quantity      int NOT NULL CONSTRAINT chk_items_qty CHECK (quantity > 0),
    unit_price    decimal(10,2) NOT NULL CONSTRAINT chk_items_price CHECK (unit_price >= 0),
    CONSTRAINT uq_order_item UNIQUE (order_id, book_id)
);
GO
INSERT INTO roles (role_name) VALUES
(N'Администратор'), (N'Продавец'), (N'Покупатель');

INSERT INTO users (login, last_name, first_name, middle_name, role_id) VALUES
(N'admin',   N'Соколова',  N'Ирина',   N'Петровна',     1),
(N'seller1', N'Морозов',   N'Алексей', N'Игоревич',     2),
(N'seller2', N'Лебедева',  N'Мария',   NULL,            2),
(N'client1', N'Волков',    N'Дмитрий', N'Сергеевич',    3),
(N'client2', N'Кузнецова', N'Анна',    N'Владимировна', 3),
(N'client3', N'Орлов',     N'Павел',   NULL,            3);

INSERT INTO genres (genre_name) VALUES
(N'Классическая проза'), (N'Поэзия'), (N'Фантастика'), (N'Детектив'), (N'Юмор и сатира');

INSERT INTO publishers (publisher_name) VALUES
(N'АСТ'), (N'Эксмо'), (N'Азбука');

INSERT INTO authors (last_name, first_name, middle_name) VALUES
(N'Пушкин', N'Александр', N'Сергеевич'),
(N'Толстой', N'Лев', N'Николаевич'),
(N'Достоевский', N'Фёдор', N'Михайлович'),
(N'Булгаков', N'Михаил', N'Афанасьевич'),
(N'Ильф', N'Илья', N'Арнольдович'),
(N'Петров', N'Евгений', N'Петрович'),
(N'Стругацкий', N'Аркадий', N'Натанович'),
(N'Стругацкий', N'Борис', N'Натанович'),
(N'Кристи', N'Агата', NULL),
(N'Чехов', N'Антон', N'Павлович');

INSERT INTO books (isbn, title, genre_id, publisher_id, publication_year, image_path, base_price, stock_quantity) VALUES
(N'978-5-0000-0001-1', N'Евгений Онегин',                   2, 3, 2023, N'book01.jpg', 450.00, 12),
(N'978-5-0000-0002-2', N'Война и мир. Том 1',               1, 2, 2022, N'book02.jpg', 890.00, 7),
(N'978-5-0000-0003-3', N'Преступление и наказание',         1, 1, 2024, N'book03.jpg', 520.00, 5),
(N'978-5-0000-0004-4', N'Мастер и Маргарита',               1, 1, 2023, N'book04.jpg', 610.00, 3),
(N'978-5-0000-0005-5', N'Двенадцать стульев',               5, 3, 2022, N'book05.jpg', 480.00, 4),
(N'978-5-0000-0006-6', N'Пикник на обочине',                3, 1, 2024, N'book06.jpg', 390.00, 1),
(N'978-5-0000-0007-7', N'Трудно быть богом',                3, 1, 2023, N'book07.jpg', 420.00, 0),
(N'978-5-0000-0008-8', N'Убийство в «Восточном экспрессе»', 4, 2, 2021, NULL,          550.00, 9),
(N'978-5-0000-0009-9', N'Дама с собачкой',                  1, 3, 2022, N'book09.jpg', 300.00, 6),
(N'978-5-0000-0010-0', N'Золотой телёнок',                  5, 3, 2023, N'book10.jpg', 495.00, 8);

INSERT INTO book_authors (book_id, author_id) VALUES
(1,1), (2,2), (3,3), (4,4), (5,5), (5,6), (6,7), (6,8),
(7,7), (7,8), (8,9), (9,10), (10,5), (10,6);

INSERT INTO orders (order_date, client_id) VALUES
('2026-09-15', 4), ('2026-10-05', 5), ('2026-10-12', 4), ('2026-10-20', 6), ('2026-11-02', 5);

INSERT INTO order_items (order_id, book_id, quantity, unit_price) VALUES
(1, 2, 1, 667.50), (1, 1, 1, 337.50),
(2, 4, 1, 457.50),
(3, 1, 2, 450.00),
(4, 6, 1, 292.50),
(5, 3, 1, 390.00);
GO
