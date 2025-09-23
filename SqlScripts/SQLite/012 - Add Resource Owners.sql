--
-- Add column `level` to `users`
--
ALTER TABLE `users` ADD `level` INTEGER NOT NULL DEFAULT 3;

--
-- Add column `owner` to `galleries`
--
ALTER TABLE `galleries` ADD `owner` INTEGER NOT NULL DEFAULT 0;

--
-- Add column `owner` to `custom_resources`
--
ALTER TABLE `custom_resources` ADD `owner` INTEGER NOT NULL DEFAULT 0;