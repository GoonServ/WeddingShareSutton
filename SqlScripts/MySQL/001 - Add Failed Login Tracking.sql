--
-- Table structure for table `users`
--
ALTER TABLE `users` ADD `failed_logins` INTEGER NOT NULL DEFAULT 0;
ALTER TABLE `users` ADD `lockout_until` BIGINT;