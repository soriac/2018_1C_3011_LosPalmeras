using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Drawing;
using TGC.Core.Camara;
using TGC.Core.Direct3D;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.Textures;
using TGC.Group.Model.Interfaz;
using TGC.Group.Model.Scenes;

namespace TGC.Group.Model.Escenas
{
    class GameVictoryEscena : Escena
    {

        private ElementoTexto textoVictoria1, textoVictoria2;
        private Boton jugarDeNuevo;
        private Sprite s;
        private TgcTexture fondo;
        private Viewport viewport = D3DDevice.Instance.Device.Viewport;
        
        public void init(string mediaDir, string shaderDir)
        {

            textoVictoria1 = new ElementoTexto("¡FELICIDADES!", 0.4f, 0);
            textoVictoria2 = new ElementoTexto("CONSEGUISTE EL TROFEO CRASH", 0.25f, 0.1f);
            jugarDeNuevo = new Boton("JUGAR DE NUEVO", 0.7f, 0.7f, () => EscenaManager.getInstance().goBack());
            s = new Sprite(D3DDevice.Instance.Device);
            fondo = TgcTexture.createTexture(D3DDevice.Instance.Device, mediaDir + "victoryFondo.jpg");

        }

        public void update(float deltaTime, TgcD3dInput input, TgcCamera camara)
        {

            jugarDeNuevo.Update(deltaTime, input);

        }

        public void render(float deltaTime)
        {

            s.Begin(SpriteFlags.AlphaBlend | SpriteFlags.SortDepthFrontToBack);

            var scaling = new TGCVector3(
                (float)viewport.Width / fondo.Width,
                (float)viewport.Height / fondo.Height,
            0);

            s.Transform = TGCMatrix.Scaling(scaling);
            s.Draw(fondo.D3dTexture, Rectangle.Empty, Vector3.Empty, Vector3.Empty, Color.White);

            s.End();

            textoVictoria1.Render();
            textoVictoria2.Render();

            jugarDeNuevo.Render();

        }


        public void dispose()
        {

            textoVictoria1.Dispose();
            textoVictoria2.Dispose();
            jugarDeNuevo.Dispose();

        }

    }
}
