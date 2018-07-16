using Microsoft.DirectX.Direct3D;
using System.Drawing;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Interpolation;
using TGC.Core.Mathematica;
using TGC.Core.Shaders;
using TGC.Core.Sound;
using TGC.Core.Terrain;
using TGC.Core.Textures;
using TGC.Group.Model.Escenas;
using TGC.Group.Model.Scenes;

namespace TGC.Group.Model {

    public class GameModel : TgcExample {

        private TgcSkyBox skyBox;
        private TgcTexture alarmTexture;
        private Surface depthStencil; // Depth-stencil buffer
        private Surface depthStencilOld;
        private Effect effect;
        private InterpoladorVaiven intVaivenAlarm;
        private Surface pOldRT;
        private Texture renderTarget2D;
        private VertexBuffer screenQuadVB;
        private bool huboColision;


        public GameModel(string mediaDir, string shadersDir) : base(mediaDir, shadersDir) {

            Category = Game.Default.Category;
            Name = Game.Default.Name;
            Description = Game.Default.Description;

        }

        public override void Init() {

            Musica.getInstance().setMusica(MediaDir + "\\NsanityBeach.mp3");
            Musica.getInstance().playDeFondo();
            Musica.getInstance().setDsDevice(DirectSound.DsDevice);

            var escenaInicial = new InicioEscena();
            //var escenaInicial = new GameEscena();
            EscenaManager.getInstance().setMediaDir(MediaDir);
            EscenaManager.getInstance().setShaderDir(ShadersDir);
            EscenaManager.getInstance().addScene(escenaInicial);

            var pathSkyBoxCaras = MediaDir + "\\SkyBoxFaces\\";

            D3DDevice.Instance.ParticlesEnabled = true;
            D3DDevice.Instance.EnableParticles();

            huboColision = false;

            CustomVertex.PositionTextured[] screenQuadVertices =
            {
                new CustomVertex.PositionTextured(-1, 1, 1, 0, 0),
                new CustomVertex.PositionTextured(1, 1, 1, 1, 0),
                new CustomVertex.PositionTextured(-1, -1, 1, 0, 1),
                new CustomVertex.PositionTextured(1, -1, 1, 1, 1)
            };
            // VBuffer de triangulos
            screenQuadVB = new VertexBuffer(typeof(CustomVertex.PositionTextured), 4, D3DDevice.Instance.Device, Usage.Dynamic | Usage.WriteOnly,
                CustomVertex.PositionTextured.Format, Pool.Default);
            screenQuadVB.SetData(screenQuadVertices, 0, LockFlags.None);

            // Render Target para la pantalla
            renderTarget2D = new Texture(D3DDevice.Instance.Device, D3DDevice.Instance.Device.PresentationParameters.BackBufferWidth,
                D3DDevice.Instance.Device.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);

            // Crear Depth Stencil
            depthStencil = D3DDevice.Instance.Device.CreateDepthStencilSurface(D3DDevice.Instance.Device.PresentationParameters.BackBufferWidth,
                    D3DDevice.Instance.Device.PresentationParameters.BackBufferHeight, DepthFormat.D24S8, MultiSampleType.None, 0, true);
            depthStencilOld = D3DDevice.Instance.Device.DepthStencilSurface;

            effect = TgcShaders.loadEffect(ShadersDir + "PostProcess.fx");
            effect.Technique = "AlarmaTechnique";

            // Cargo textura de alarma
            alarmTexture = TgcTexture.createTexture(D3DDevice.Instance.Device, MediaDir + "efecto_alarma.png");

            //Interpolador para efecto de variar la intensidad de la textura de alarma
            intVaivenAlarm = new InterpoladorVaiven();
            intVaivenAlarm.Min = 0;
            intVaivenAlarm.Max = 1;
            intVaivenAlarm.Speed = 5;
            intVaivenAlarm.reset();

        }

        public override void Update() {

            PreUpdate();

            EscenaManager.getInstance().update(ElapsedTime, Input, Camara, ref huboColision);

            PostUpdate();
        }

        public override void Render()
        {
            ClearTextures();

            pOldRT = D3DDevice.Instance.Device.GetRenderTarget(0);
            var pSurf = renderTarget2D.GetSurfaceLevel(0);
            D3DDevice.Instance.Device.SetRenderTarget(0, pSurf);

            D3DDevice.Instance.Device.DepthStencilSurface = depthStencil;
            D3DDevice.Instance.Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);

            drawSceneToRenderTarget(D3DDevice.Instance.Device);

            pSurf.Dispose();

            D3DDevice.Instance.Device.SetRenderTarget(0, pOldRT);
            D3DDevice.Instance.Device.DepthStencilSurface = depthStencilOld;

            drawPostProcess(D3DDevice.Instance.Device, ElapsedTime);
        }

       
        private void drawSceneToRenderTarget(Device d3dDevice)
        {
            d3dDevice.BeginScene();

            EscenaManager.getInstance().render(ElapsedTime);
                     
            d3dDevice.EndScene();
        }

        
        private void drawPostProcess(Device d3dDevice, float elapsedTime)
        {
            d3dDevice.BeginScene();
                      
            d3dDevice.VertexFormat = CustomVertex.PositionTextured.Format;
            d3dDevice.SetStreamSource(0, screenQuadVB, 0);

            // Me fijo si colisiono con una caja
            if (huboColision)
            {
                effect.Technique = "AlarmaTechnique";
                huboColision = false;
            }
            else
            {
                effect.Technique = "DefaultTechnique";
            }

            // Cargar parametros al PPS
            effect.SetValue("render_target2D", renderTarget2D);
            effect.SetValue("textura_alarma", alarmTexture.D3dTexture);
            effect.SetValue("alarmaScaleFactor", intVaivenAlarm.update(elapsedTime));

            d3dDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            effect.Begin(FX.None);
            effect.BeginPass(0);
            d3dDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            effect.EndPass();
            effect.End();

            RenderFPS();
            RenderAxis();
            d3dDevice.EndScene();
            d3dDevice.Present();
        }

        public override void Dispose() {

            EscenaManager.getInstance().dispose();

        }
    }
}