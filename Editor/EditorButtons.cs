using System.Collections.Generic;
using System.Linq;
using Editor;
using Sandbox;

namespace Battlebugs;

internal static class EditorButtons
{

    [Menu( "Editor", "Battlebugs/Generate Thumbnails", Icon = "camera" )]
    static void GenerateThumbnails()
    {
        var bugs = ResourceLibrary.GetAll<BugResource>();
        var models = new List<ThumbnailEntry>();

        foreach ( var bug in bugs )
        {
            var name = bug.ResourceName;
            models.Add( new ThumbnailEntry( bug.HeadModel, name + "_head" ) );
            models.Add( new ThumbnailEntry( bug.BodyModel, name + "_body" ) );
            models.Add( new ThumbnailEntry( bug.CornerModel, name + "_corner" ) );
            models.Add( new ThumbnailEntry( bug.TailModel, name + "_tail" ) );
        }

        foreach ( var entry in models )
        {
            GenerateThumbnail( entry.Model, entry.Name );
        }
    }

    static void GenerateThumbnail( Model model, string fileName )
    {
        var sceneWorld = new SceneWorld();
        var sceneModel = new SceneModel( sceneWorld, model, new() );
        var sceneCamera = new SceneCamera();
        var sceneLight = new SceneDirectionalLight( sceneWorld, Rotation.From( 0, 90, 0 ), Color.White );
        sceneCamera.Ortho = true;
        sceneCamera.OrthoHeight = 64f;
        sceneCamera.World = sceneWorld;
        sceneCamera.Rotation = Rotation.From( 0, 90, 0 );
        sceneModel.Rotation = Rotation.From( 0, 180, 0 );
        if ( model.Name.Contains( "corner" ) ) sceneModel.Rotation *= new Angles( 0, 0, 90 );
        else if ( !model.Name.Contains( "caterpillar" ) ) sceneModel.Rotation *= new Angles( 0, 90, 0 );

        if ( fileName.Contains( "ladybug" ) ) sceneCamera.OrthoHeight = 42f;
        else if ( fileName.Contains( "bumblebee" ) )
        {
            sceneCamera.OrthoHeight = 82f;
            sceneCamera.Rotation = Rotation.From( 0, 180, 0 );
            sceneLight.Rotation = Rotation.From( 0, 180, 0 );
        }
        else if ( fileName.Contains( "dragonfly" ) )
        {
            sceneCamera.OrthoHeight = 82f;
            sceneCamera.Rotation = Rotation.From( 0, 180, 0 );
            sceneLight.Rotation = Rotation.From( 0, 180, 0 );
        }

        var bounds = sceneModel.Bounds;
        var center = bounds.Center;
        sceneCamera.Position = sceneCamera.Rotation.Backward * 92f;
        if ( fileName.Contains( "caterpillar_head" ) ) sceneCamera.Position += Vector3.Up * 18f;
        else sceneCamera.Position += center;

        var texture = Texture.CreateRenderTarget().WithSize( 128, 128 ).Create();
        Graphics.RenderToTexture( sceneCamera, texture );
        var pixelMap = Pixmap.FromTexture( texture );
        var path = Editor.FileSystem.Content.GetFullPath( "" ) + "\\UI\\thumbnails\\" + fileName + ".png";
        pixelMap.SavePng( path );

        texture.Dispose();
        sceneLight.Delete();
        sceneCamera.Dispose();
        sceneModel.Delete();
        sceneWorld.Delete();
    }

    class ThumbnailEntry
    {
        public Model Model { get; set; }
        public string Name { get; set; }

        public ThumbnailEntry( Model model, string name )
        {
            Model = model;
            Name = name;
        }
    }

}