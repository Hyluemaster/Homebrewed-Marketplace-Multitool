CREATE TABLE IF NOT EXISTS books (
    id INT NOT NULL AUTO_INCREMENT,
    name VARCHAR(255) NOT NULL,
    content LONGBLOB NULL,
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARACTER SET utf8mb4;

-- For an existing books table, back up its data before changing content to binary:
-- ALTER TABLE books MODIFY content LONGBLOB NULL;
