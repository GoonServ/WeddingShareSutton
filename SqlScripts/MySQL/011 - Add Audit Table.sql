--
-- Table structure for table `audit_logs`
--
DROP TABLE IF EXISTS `audit_logs`;
CREATE TABLE `audit_logs` (
  `id` INT NOT NULL PRIMARY KEY AUTO_INCREMENT,
  `message` VARCHAR(500) NOT NULL,
  `username` VARCHAR(50) NOT NULL,
  `timestamp` DATETIME NOT NULL
);