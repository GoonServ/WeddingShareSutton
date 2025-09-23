--
-- Add column `level` to `users`
--
ALTER TABLE `users` ADD `level` INT NOT NULL DEFAULT 3;

--
-- Add column `owner` to `galleries`
--
ALTER TABLE `galleries` ADD `owner` INT NOT NULL DEFAULT 0;

--
-- Add column `owner` to `custom_resources`
--
ALTER TABLE `custom_resources` ADD `owner` INT NOT NULL DEFAULT 0;