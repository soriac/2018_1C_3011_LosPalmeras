using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX.DirectInput;
using System.Drawing;
using TGC.Core.Camara;
using TGC.Core.Collision;
using TGC.Core.Direct3D;
using TGC.Core.Geometry;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.Shaders;
using TGC.Core.Textures;
using TGC.Group.Model.Niveles;

namespace TGC.Group.Model.Scenes {
    class GameEscena : Escena {
        private Personaje personaje;
        private Nivel nivel;
        private TGCVector3 cameraOffset;

        private TGCVector3 VEC_GRAVEDAD = new TGCVector3(0, -25f, 0);

        private Sprite hud;
        private TgcTexture barraStamina;
        private TgcTexture unidadStamina;
        private TgcTexture vida;

        private Microsoft.DirectX.Direct3D.Effect smEffect;
        private readonly int SM_SIZE = 1024;
        private readonly float NEAR_PLANE = 2;
        private readonly float FAR_PLANE = 5000;
        private Texture g_pShadowMap;
        private Surface g_pDDSShadow;
        private TGCMatrix g_mShadowProj;

        private TgcD3dInput auxInput;

        public void init(string mediaDir, string shaderDir) {
            cameraOffset = new TGCVector3(0, 200, 400);
            setNivel(new Nivel1(mediaDir));
            personaje = new Personaje(mediaDir, shaderDir);

            hud = new Sprite(D3DDevice.Instance.Device);
            barraStamina = TgcTexture.createTexture(mediaDir + "stamina.png");
            unidadStamina = TgcTexture.createTexture(mediaDir + "staminaUnidad.png");
            vida = TgcTexture.createTexture(mediaDir + "vida.png");

            setupShadowMap(shaderDir);
        }

        public void setNivel(Nivel nuevoNivel) {
            if (nivel != null) nivel.dispose();
            if (personaje != null) personaje.resetear();
            nivel = nuevoNivel;
        }

        public void update(float deltaTime, TgcD3dInput input, TgcCamera camara) {

            if (deltaTime > 1) return;

            auxInput = input;

            // calculo nueva velocidad
            personaje.update(deltaTime, input);

            checkearEmpujeCajas();
            aplicarGravedadCajas();

            personaje.move(deltaTime, nivel, VEC_GRAVEDAD);

            nivel.update(deltaTime);

            // Checkear si toque la levelFinishBox
            if (nivel.getLFBox() != null && TgcCollisionUtils.testSphereAABB(personaje.getBoundingSphere(), nivel.getLFBox())) {
                if (nivel.siguienteNivel == null) {
                    EscenaManager.getInstance().goBack();
                } else {
                    nivel.siguienteNivel.setEffect(smEffect);
                    setNivel(nivel.siguienteNivel);
                }
            }

            checkearMuerte();

            if (personaje.getPosition().Y < -1500) personaje.morir();

            // tecla de reset
            if (input.keyPressed(Key.F9)) personaje.morir();

            camara.SetCamera(personaje.getPosition() + cameraOffset, personaje.getPosition());
            smEffect.SetValue("g_vPosEye", new Vector4(camara.Position.X, camara.Position.Y, camara.Position.Z, 1));
        }

        public void render(float deltaTime) {

            if (deltaTime > 1) return;

            renderShadowMap(deltaTime);

            hud.Begin(SpriteFlags.AlphaBlend | SpriteFlags.SortDepthFrontToBack);
            hud.Transform = TGCMatrix.Scaling(TGCVector3.One);
            hud.Draw(barraStamina.D3dTexture, Rectangle.Empty, Vector3.Empty, Vector3.Empty, Color.White);
            hud.Transform = TGCMatrix.Scaling(new TGCVector3(personaje.getStamina() / 200f * 256f, 2, 1));
            hud.Draw(unidadStamina.D3dTexture, Rectangle.Empty, Vector3.Empty, Vector3.Empty, Color.White);

            int posVidas = D3DDevice.Instance.Device.Viewport.Width - vida.Width;

            for (int i = 0; i < personaje.getVidas(); i++) {
                hud.Transform = TGCMatrix.Translation(new TGCVector3(posVidas, 0, 0));
                hud.Draw(vida.D3dTexture, Rectangle.Empty, Vector3.Empty, Vector3.Empty, Color.White);
                posVidas -= vida.Width;
            }


            hud.End();
        }

        private void setupShadowMap(string shaderDir) {
            smEffect = TgcShaders.loadEffect(shaderDir + "ShadowMap.fx");
            nivel.setEffect(smEffect);

            g_pShadowMap = new Texture(D3DDevice.Instance.Device, SM_SIZE, SM_SIZE, 1, Usage.RenderTarget, Format.R32F, Pool.Default);
            g_pDDSShadow = D3DDevice.Instance.Device.CreateDepthStencilSurface(SM_SIZE, SM_SIZE, DepthFormat.D24S8, MultiSampleType.None, 0, true);

            g_mShadowProj = TGCMatrix.PerspectiveFovLH(Geometry.DegreeToRadian(80), D3DDevice.Instance.AspectRatio, 50, 5000); 
        }

        private void renderShadowMap(float deltaTime) {
            var offset = new TGCVector3(50, 150, 300);
            var lightPos = personaje.getPosition() + offset;
            var lightDir = -offset;
            lightDir.Normalize();

            smEffect.SetValue("g_vLightPos", new Vector4(lightPos.X, lightPos.Y, lightPos.Z, 1));
            smEffect.SetValue("g_vLightDir", new Vector4(lightDir.X, lightDir.Y, lightDir.Z, 1));
            var g_lightView = TGCMatrix.LookAtLH(lightPos, lightPos + lightDir, new TGCVector3(0, 0, 1));

            smEffect.SetValue("g_mProjLight", g_mShadowProj.ToMatrix());
            smEffect.SetValue("g_mViewLightProj", (g_lightView * g_mShadowProj).ToMatrix());

            var oldRT = D3DDevice.Instance.Device.GetRenderTarget(0);
            var shadowSurf = g_pShadowMap.GetSurfaceLevel(0);
            D3DDevice.Instance.Device.SetRenderTarget(0, shadowSurf);

            var oldDS = D3DDevice.Instance.Device.DepthStencilSurface;
            D3DDevice.Instance.Device.DepthStencilSurface = g_pDDSShadow;
            D3DDevice.Instance.Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);

            smEffect.SetValue("g_txShadow", g_pShadowMap);

            nivel.setTechnique("RenderShadow");

            nivel.render();
            personaje.prepareForShadowMapping(lightPos, lightDir, g_mShadowProj, g_lightView, g_pShadowMap);
            personaje.render(deltaTime);

            if (auxInput.keyDown(Key.F5))
                TextureLoader.Save("shadowmap.jpg", ImageFileFormat.Jpg, g_pShadowMap);

            D3DDevice.Instance.Device.EndScene();
            D3DDevice.Instance.Device.DepthStencilSurface = oldDS;
            D3DDevice.Instance.Device.SetRenderTarget(0, oldRT);

            D3DDevice.Instance.Device.BeginScene();
            D3DDevice.Instance.Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);

            nivel.setTechnique("RenderScene");
            nivel.render();

            personaje.prepareForNormalRender();
            personaje.render(deltaTime);

            var flecha = new TgcArrow();
            flecha.Thickness = 2f;
            flecha.HeadSize = new TGCVector2(20f, 20f);
            flecha.HeadColor = Color.DarkRed;
            flecha.BodyColor = Color.Red;
            flecha.PStart = lightPos;
            flecha.PEnd = lightPos + lightDir * 20f;
            flecha.updateValues();
            //flecha.Render();
        }

        public void dispose() {
            personaje.dispose();
            nivel.dispose();
            unidadStamina.dispose();
            barraStamina.dispose();
        }

        private void checkearEmpujeCajas() {

            foreach (var caja in nivel.getCajas()) {

                if (TgcCollisionUtils.testSphereAABB(personaje.getBoundingSphere(), caja.getCuerpo())) {

                    // Direccion en la que estoy queriendo mover la caja
                    var centroCaja = caja.getCuerpo().calculateBoxCenter();
                    var centroPersonaje = personaje.getBoundingSphere().Center;
                    var distanciaPersonajeCaja = centroCaja - centroPersonaje;

                    // Hacia donde va a moverse finalmente la caja
                    var cajaMovementDeseado = TGCVector3.Empty;

                    // Lo guardo en otra estructura para no joder a distanciaPersonajeCaja; lo necesito normalizado
                    var direccionRayo = distanciaPersonajeCaja;
                    direccionRayo.Normalize();

                    // Rayo con la direccion en la que quiero mover la caja
                    TgcRay rayoMovimiento = new TgcRay();
                    //rayoMovimiento.Origin = centroPersonaje;
                    rayoMovimiento.Direction = direccionRayo;

                    // Solo si no colisiona con ningun estatico; TODO: Implementar colision con otras cajas
                    if(!checkearColisionCajaEstaticos(caja, rayoMovimiento))
                    {
                        if (FastMath.Abs(distanciaPersonajeCaja.X) > FastMath.Abs(distanciaPersonajeCaja.Z))
                        {
                            if (distanciaPersonajeCaja.X > 0)      // Empujo desde la izquierda
                            {
                                cajaMovementDeseado = new TGCVector3(5, 0, 0);
                            }
                            else                                   // Empujo desde la derecha
                            {
                                cajaMovementDeseado = new TGCVector3(-5, 0, 0);
                            }
                        }
                        else
                        {
                            if (distanciaPersonajeCaja.Z > 0)      // Empujo desde el frente 
                            {
                                cajaMovementDeseado = new TGCVector3(0, 0, 5);
                            }
                            else                                   // Empujo desde la parte de atras
                            {
                                cajaMovementDeseado = new TGCVector3(0, 0, -5);
                            }
                        }
                    }

                    caja.move(cajaMovementDeseado); //TEMP
                    //personaje.playSound(3);
                }
            }
        }

        // Checkeo si la caja esta colisionando con una plataforma estatica o pared en
        // la direccion del rayoEmpuje. Devuelve true si lo esta haciendo, false si no
        private bool checkearColisionCajaEstaticos(Caja unaCaja, TgcRay rayoEmpuje)
        {

            var centroCaja = unaCaja.getCuerpo().calculateBoxCenter();
            var finCaja = centroCaja + rayoEmpuje.Direction * 80f;

            foreach(var estatico in nivel.getEstaticos())
            {

                TGCVector3 auxiliar = new TGCVector3();

                if (TgcCollisionUtils.intersectSegmentAABB(centroCaja, finCaja, estatico, out auxiliar))
                {
                    return true;
                }
                               
            }

            return false;

        }

        private void aplicarGravedadCajas() {
            foreach (var caja in nivel.getCajas()) {
                var apoyada = false;
                foreach (var piso in nivel.getPisos()) {
                    if (TgcCollisionUtils.testAABBAABB(caja.getCuerpo(), piso)) {
                        apoyada = true;
                    }
                }

                if (!apoyada) {
                    caja.addVel(VEC_GRAVEDAD);
                } else {
                    caja.resetVel();
                }

                caja.applyGravity();
            }
        }

        private void checkearMuerte() {
            foreach (var box in nivel.getDeathPlanes()) {
                if (TgcCollisionUtils.testSphereAABB(personaje.getBoundingSphere(), box)) {
                    personaje.morir();
                    // TODO: death
                }
            }
        }

    }
} 