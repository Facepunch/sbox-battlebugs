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
            if ( !models.Any( x => x.Model == bug.HeadModel ) ) models.Add( new ThumbnailEntry( bug.HeadModel, name + "_head" ) );
            if ( !models.Any( x => x.Model == bug.BodyModel ) ) models.Add( new ThumbnailEntry( bug.BodyModel, name + "_body" ) );
            if ( !models.Any( x => x.Model == bug.CornerModel ) ) models.Add( new ThumbnailEntry( bug.CornerModel, name + "_corner" ) );
            if ( !models.Any( x => x.Model == bug.TailModel ) ) models.Add( new ThumbnailEntry( bug.TailModel, name + "_tail" ) );
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
        sceneCamera.World = sceneWorld;
        sceneCamera.Rotation = Rotation.From( 0, 90, 0 );
        sceneModel.Rotation = Rotation.From( 0, 180, 0 );
        if ( model.Name.Contains( "corner" ) ) sceneModel.Rotation *= new Angles( 0, 0, 90 );
        else if ( !model.Name.Contains( "caterpillar" ) ) sceneModel.Rotation *= new Angles( 0, 90, 0 );

        var bounds = sceneModel.Bounds;
        var center = bounds.Center;
        var distance = bounds.Size.Length * 0.8f;
        sceneCamera.Position = center + sceneCamera.Rotation.Backward * distance;

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