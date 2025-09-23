--
-- Table structure for table `audit_logs`
--
DROP TABLE IF EXISTS `audit_logs`;
CREATE TABLE `audit_logs` (
  `id` INTEGER NOT NULL PRIMARY KEY,
  `message` TEXT NOT NULL,
  `username` TEXT NOT NULL,
  `timestamp` INTEGER NOT NULL
);