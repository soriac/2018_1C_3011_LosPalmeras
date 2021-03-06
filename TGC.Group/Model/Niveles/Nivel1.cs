﻿using TGC.Core.Direct3D;
using TGC.Core.Geometry;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Textures;
using TGC.Group.Model.Niveles;

//NIVEL INICIAL, JUNGLA

namespace TGC.Group.Model.Niveles {

    class Nivel1 : Nivel {

        TgcTexture piso, limites, trineo, piedra, tronco;
        TgcScene[] escenasBananas;
        TgcScene[] escenasSelvaticos;
        TgcScene[] escenasPalmeras;
        TgcScene[] escenasRocas;
        private TgcMesh arbolBananas;
        private TgcMesh arbolSelvatico;
        private TgcMesh palmera;
        private TgcMesh roca;

        public Nivel1(string mediaDir) : base(mediaDir) {
        }

        public override void init(string mediaDir) {

            //SkyBox a inicializar
            pathSkyBox = mediaDir + "\\SkyBoxes\\SkyBoxSelva\\";
            inicializarSkyBox(pathSkyBox);

            // Texturas empleadas
            piso = TgcTexture.createTexture(D3DDevice.Instance.Device, mediaDir + "pisoJungla.jpg");
            texturasUsadas.Add(piso);
            limites = TgcTexture.createTexture(D3DDevice.Instance.Device, mediaDir + "paredJungla.jpg");
            texturasUsadas.Add(limites);
            piedra = TgcTexture.createTexture(D3DDevice.Instance.Device, mediaDir + "piedra.jpg");
            texturasUsadas.Add(piedra);
            tronco = TgcTexture.createTexture(D3DDevice.Instance.Device, mediaDir + "tronco.jpg");
            texturasUsadas.Add(tronco);
            trineo = TgcTexture.createTexture(D3DDevice.Instance.Device, mediaDir + "Trineo.png");
            texturasUsadas.Add(trineo);

            // Bloques de piso (no precipicios)
            agregarPisoNormal(new TGCVector3(-700, 0, 8000), new TGCVector3(1400, 0, 2000), piso);
            agregarPisoNormal(new TGCVector3(-700, 0, 5000), new TGCVector3(1400, 0, 2000), piso);
            agregarPisoNormal(new TGCVector3(-400, 0, 3500), new TGCVector3(800, 0, 1500), piso);
            agregarPisoNormal(new TGCVector3(-700, 0, 2000), new TGCVector3(1400, 0, 1500), piso);
            agregarPisoNormal(new TGCVector3(-700, 0, 0), new TGCVector3(1400, 0, 1000), piso);

            // Limites del pasillo-escenario
            // DEFINIR: Van en precipicio tambien o no?
            agregarPared(new TGCVector3(710, 40, 5000), new TGCVector3(20, 80, 10000), limites);  // limite izquierdo
            agregarPared(new TGCVector3(-710, 40, 5000), new TGCVector3(20, 80, 10000), limites); // limite derecho
            agregarPared(new TGCVector3(0, 40, 9990), new TGCVector3(1400, 80, 20), limites);     // frente
            agregarPared(new TGCVector3(0, 40, 10), new TGCVector3(1400, 80, 20), limites);       // fondo

            // Escaleras con plataforma de madera en el medio
            int desp;
            agregarEscaleraLateralIzquierda(new TGCVector3(0, 5, 6835), 15, new TGCVector3(80, 10, 20), piedra);
            agregarPared(new TGCVector3(340, 75, 6835), new TGCVector3(100, 150, 80), tronco);      //1º Pilar izq.
            agregarPared(new TGCVector3(340, 140, 6725), new TGCVector3(100, 20, 300), tronco);
            agregarPared(new TGCVector3(340, 75, 6525), new TGCVector3(100, 150, 100), tronco);     //2º Pilar izq,
            for(desp = 0; desp < 10; desp++)                // Pasarela del medio
            {
                agregarPared(new TGCVector3(351 - (desp * 78), 140, 6525), new TGCVector3(78, 20, 100), tronco);
            }
            agregarPared(new TGCVector3(-340, 75, 6525), new TGCVector3(100, 150, 100), tronco);     //1º Pilar der.
            agregarPared(new TGCVector3(-340, 140, 6325), new TGCVector3(100, 20, 300), tronco);
            agregarPared(new TGCVector3(-340, 75, 6215), new TGCVector3(100, 150, 80), tronco);     //2º Pilar der.
            agregarEscaleraLateralDerecha(new TGCVector3(0, 5, 6215), 15, new TGCVector3(80, 10, 20), piedra);

            // Cajas empujables
            cajas.Add(new Caja(mediaDir, new TGCVector3(300, 40, 9000)));
            cajas.Add(new Caja(mediaDir, new TGCVector3(-300, 40, 9000)));
            cajas.Add(new Caja(mediaDir, new TGCVector3(0, 40, 4000)));

            // Plataformas para precipicios
            pDesplazan.Add(new PlataformaDesplazante(new TGCVector3(0, -25, 7900), new TGCVector3(200, 50, 200), caja, new TGCVector3(0, -25, 7100), new TGCVector3(0, 0, VELPEPE))); // Precipicio 1, desplaza en Z
            pDesplazan.Add(new PlataformaDesplazante(new TGCVector3(400, -25, 1950), new TGCVector3(200, 50, 100), caja, new TGCVector3(-4000, -25, 1150), new TGCVector3(VELPEPE, 0, VELPEPE))); // Precipicio 2, desplaza en XZ

            // Plataforma rotante del final
            pRotantes.Add(new PlataformaRotante(new TGCVector3(0, 50, 300), new TGCVector3(200, 50, 200), caja, FastMath.PI * 100));

            // Scenes para objetos decorativos
            escenasBananas = new TgcScene[6];
            for(int i = 0; i <= 5; i++) {
                escenasBananas[i] = loaderDeco.loadSceneFromFile(mediaDir + "\\Decorativos\\ArbolBananas\\ArbolBananas-TgcScene.xml");
            }
            escenasPalmeras = new TgcScene[4];
            for (int i = 0; i <= 3; i++)
            {
                escenasPalmeras[i] = loaderDeco.loadSceneFromFile(mediaDir + "\\Decorativos\\Palmera\\Palmera-TgcScene.xml");
            }
            escenasSelvaticos = new TgcScene[2];
            for (int i = 0; i <= 1; i++)
            {
                escenasSelvaticos[i] = loaderDeco.loadSceneFromFile(mediaDir + "\\Decorativos\\ArbolSelvatico2\\ArbolSelvatico2-TgcScene.xml");
            }
            escenasRocas = new TgcScene[8];
            for (int i = 0; i <= 7; i++)
            {
                escenasRocas[i] = loaderDeco.loadSceneFromFile(mediaDir + "\\Decorativos\\Roca\\Roca-TgcScene.xml");
            }

            // Objetos decorativos
            // Arboles de bananas
            cargarDecorativo(arbolBananas, escenasBananas[0], new TGCVector3(600, 0, 9300), new TGCVector3(1.5f, 1.5f, 1.5f), 0);
            cargarDecorativo(arbolBananas, escenasBananas[1], new TGCVector3(600, 0, 8700), new TGCVector3(1.5f, 1.5f, 1.5f), 0);
            cargarDecorativo(arbolBananas, escenasBananas[2], new TGCVector3(-600, 0, 9300), new TGCVector3(1.5f, 1.5f, 1.5f), 0);
            cargarDecorativo(arbolBananas, escenasBananas[3], new TGCVector3(-600, 0, 8700), new TGCVector3(1.5f, 1.5f, 1.5f), 0);
            cargarDecorativo(arbolBananas, escenasBananas[4], new TGCVector3(500, 0, 2400), new TGCVector3(1.5f, 1.5f, 1.5f), 0);
            cargarDecorativo(arbolBananas, escenasBananas[5], new TGCVector3(-500, 0, 2400), new TGCVector3(1.5f, 1.5f, 1.5f), 0);
            // Arboles selvaticos
            cargarDecorativo(arbolSelvatico, escenasSelvaticos[0], new TGCVector3(600, 0, 5400), new TGCVector3(5, 5, 5), 0);
            cargarDecorativo(arbolSelvatico, escenasSelvaticos[1], new TGCVector3(-600, 0, 5400), new TGCVector3(5, 5, 5), 0);
            // Palmeras
            cargarDecorativo(palmera, escenasPalmeras[0], new TGCVector3(600, 0, 850), new TGCVector3(0.7f, 0.7f, 0.7f), 0);
            cargarDecorativo(palmera, escenasPalmeras[1], new TGCVector3(-600, 0, 850), new TGCVector3(0.7f, 0.7f, 0.7f), 0);
            cargarDecorativo(palmera, escenasPalmeras[2], new TGCVector3(600, 0, 2150), new TGCVector3(0.7f, 0.7f, 0.7f), 0);
            cargarDecorativo(palmera, escenasPalmeras[3], new TGCVector3(-600, 0, 2150), new TGCVector3(0.7f, 0.7f, 0.7f), 0);
            // Rocas
            cargarDecorativo(roca, escenasRocas[0], new TGCVector3(300, 0, 3800), new TGCVector3(1, 1, 1), 0);
            cargarDecorativo(roca, escenasRocas[1], new TGCVector3(-300, 0, 3800), new TGCVector3(1, 1, 1), 0);
            cargarDecorativo(roca, escenasRocas[2], new TGCVector3(300, 0, 4100), new TGCVector3(2, 3, 2), 0);
            cargarDecorativo(roca, escenasRocas[3], new TGCVector3(-300, 0, 4100), new TGCVector3(2, 3, 2), 0);
            cargarDecorativo(roca, escenasRocas[4], new TGCVector3(300, 0, 4400), new TGCVector3(2, 3, 2), 0);
            cargarDecorativo(roca, escenasRocas[5], new TGCVector3(-300, 0, 4400), new TGCVector3(2, 3, 2), 0);
            cargarDecorativo(roca, escenasRocas[6], new TGCVector3(300, 0, 4700), new TGCVector3(1, 1, 1), 0);
            cargarDecorativo(roca, escenasRocas[7], new TGCVector3(-300, 0, 4700), new TGCVector3(1, 1, 1), 0);

            agregarLFBox(trineo);

            siguienteNivel = new Nivel2(mediaDir);

        }

    }

}
