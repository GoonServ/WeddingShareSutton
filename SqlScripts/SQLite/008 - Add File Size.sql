--
-- Add column `file_size` to `gallery_items`
--
ALTER TABLE `gallery_items` ADD `file_size` INTEGER NOT NULL DEFAULT 0;