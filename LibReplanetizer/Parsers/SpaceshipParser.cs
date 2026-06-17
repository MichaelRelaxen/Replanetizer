// Copyright (C) 2018-2026, The Replanetizer Contributors.
// Replanetizer is free software: you can redistribute it
// and/or modify it under the terms of the GNU General Public
// License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// Please see the LICENSE.md file for more details.

using LibReplanetizer.Headers;
using LibReplanetizer.LevelObjects;
using LibReplanetizer.Models;
using System;
using System.Collections.Generic;
using System.IO;
namespace LibReplanetizer.Parsers
{
    public class SpaceshipParser : RatchetFileParser, IDisposable
    {
        private readonly SpaceshipHeader header;
        private readonly GameType game;
        public SpaceshipParser(GameType game, string spaceshipFile, int spaceshipNum) : base(spaceshipFile)
        {
            this.game = game;
            header = new SpaceshipHeader(fileStream, spaceshipNum);
        }

        public MobyModel GetShipModel() => new MobyModel(fileStream, game, header.shipModelID, header.shipModelPointer);
        public MobyModel GetCockpitModel() => new MobyModel(fileStream, game, header.cockpitModelID, header.cockpitModelPointer);
        public List<Texture> GetTextures() => GetTextures(header.texturePointer, header.textureCount);

        public static (List<MobyModel> models, List<Texture> textures) GetAllSpaceshipData(GameType game, string enginePath, int textureIdOffset)
        {
            List<MobyModel> models = new List<MobyModel>();
            List<Texture> textures = new List<Texture>();

            foreach (var (spaceshipNum, path) in SpaceshipHeader.FindSpaceshipFiles(game, enginePath))
            {
                using (SpaceshipParser parser = new SpaceshipParser(game, path, spaceshipNum))
                {
                    MobyModel shipModel = parser.GetShipModel();
                    MobyModel cockpitModel = parser.GetCockpitModel();

                    int fileTextureIdOffset = textureIdOffset + textures.Count;

                    foreach (TextureConfig conf in shipModel.textureConfig)
                        conf.id += fileTextureIdOffset;
                    foreach (TextureConfig conf in cockpitModel.textureConfig)
                        conf.id = 1 + fileTextureIdOffset;

                    List<Texture> fileTextures = parser.GetTextures();
                    string vramPath = Path.ChangeExtension(path, ".vram");

                    using (VramParser vramParser = new VramParser(vramPath))
                        vramParser.GetTextures(fileTextures);

                    models.Add(shipModel);
                    models.Add(cockpitModel);
                    textures.AddRange(fileTextures);
                }
            }

            return (models, textures);
        }

        public void Dispose()
        {
            fileStream.Close();
        }
    }
}
