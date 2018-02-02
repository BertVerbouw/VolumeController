package com.verbouw.bert.volumecontroller;

import android.content.Context;
import android.os.Bundle;
import android.os.Parcelable;
import android.support.design.widget.Snackbar;
import android.util.Log;
import android.util.SparseArray;
import android.view.LayoutInflater;
import android.view.View;
import android.widget.ImageButton;
import android.widget.LinearLayout;
import android.widget.SeekBar;
import android.widget.TextView;

import com.android.volley.Request;
import com.android.volley.RequestQueue;
import com.android.volley.Response;
import com.android.volley.VolleyError;
import com.android.volley.toolbox.JsonArrayRequest;
import com.android.volley.toolbox.Volley;

import org.json.JSONArray;

/**
 * Created by Bert on 18/01/2018.
 */

public class VolumeController extends LinearLayout {

    private ImageButton muteButton;
    private TextView nameView;
    private SeekBar seekBar;
    private boolean isMuted;
    private int volume;
    private String name;
    private int pid;
    private Context context;

    public VolumeController(Context context, boolean isMuted, int volume, String name, int pid) {
        super(context);
        this.isMuted = isMuted;
        this.volume = volume;
        this.name = name;
        this.pid = pid;
        this.context = context;
        initializeViews(context);
    }

    /**
     * Inflates the views in the layout.
     *
     * @param context
     *           the current context for the view.
     */
    private void initializeViews(Context context) {
        LayoutInflater inflater = (LayoutInflater) context
                .getSystemService(Context.LAYOUT_INFLATER_SERVICE);
        inflater.inflate(R.layout.volume_controller, this);
        onFinishInflate();
    }

    @Override
    protected void onFinishInflate() {
        super.onFinishInflate();
        initializeValues();
    }

    public int getPid(){
        return pid;
    }

    private SeekBar.OnSeekBarChangeListener SeekBarChangeListener = new SeekBar.OnSeekBarChangeListener() {
        @Override
        public void onProgressChanged(SeekBar seekBar, int i, boolean b) {
            volume = i;
            setVolumeApi();
            setButtonImage();
        }

        @Override
        public void onStartTrackingTouch(SeekBar seekBar) {

        }

        @Override
        public void onStopTrackingTouch(SeekBar seekBar) {

        }
    };

    private void setVolumeApi() {
        ((MainActivity)context).setVolume(pid, volume);
    }

    private void setMuteApi() {
        ((MainActivity)context).setMute(pid, isMuted);
    }

    private OnClickListener MuteButtonListener = new OnClickListener() {
        @Override
        public void onClick(View view) {
            isMuted = !isMuted;
            setButtonImage();
            setMuteApi();
        }
    };

    public void initializeValues() {
        muteButton = this.findViewById(R.id.muteButton);
        nameView = this.findViewById(R.id.nameView);
        seekBar = this.findViewById(R.id.seekBar);

        muteButton.setOnClickListener(MuteButtonListener);
        seekBar.setOnSeekBarChangeListener(SeekBarChangeListener);

        nameView.setText(name);
        seekBar.setProgress(volume);
        setButtonImage();
    }

    private void setButtonImage() {
        int resource = R.drawable.ic_volume_off_black_24px;
        if(!isMuted){
            if(volume == 0){
                resource = R.drawable.ic_volume_mute_black_24px;
            }else if(volume < 50){
                resource = R.drawable.ic_volume_down_black_24px;
            }else{
                resource = R.drawable.ic_volume_up_black_24px;
            }
        }
        muteButton.setImageResource(resource);
    }


    /**
     * Identifiers for the states to save
     */
    private static String STATE_VOLUME = "Volume";
    private static String STATE_NAME = "Name";
    private static String STATE_ISMUTED = "IsMuted";

    /**
     * Identifier for the state of the super class.
     */
    private static String STATE_SUPER_CLASS = "SuperClass";

    @Override
    protected Parcelable onSaveInstanceState() {
        Bundle bundle = new Bundle();

        bundle.putParcelable(STATE_SUPER_CLASS,
                super.onSaveInstanceState());
        bundle.putInt(STATE_VOLUME, volume);
        bundle.putBoolean(STATE_ISMUTED, isMuted);
        bundle.putString(STATE_NAME, name);

        return bundle;
    }

    @Override
    protected void onRestoreInstanceState(Parcelable state) {
        if (state instanceof Bundle) {
            Bundle bundle = (Bundle)state;

            super.onRestoreInstanceState(bundle
                    .getParcelable(STATE_SUPER_CLASS));
            isMuted = bundle.getBoolean(STATE_ISMUTED);
            name = bundle.getString(STATE_NAME);
            volume = bundle.getInt(STATE_VOLUME);
            initializeValues();
        }
        else
            super.onRestoreInstanceState(state);
    }

    @Override
    protected void dispatchSaveInstanceState(SparseArray<Parcelable> container) {
        // Makes sure that the state of the child views in the side
        // spinner are not saved since we handle the state in the
        // onSaveInstanceState.
        super.dispatchFreezeSelfOnly(container);
    }

    @Override
    protected void dispatchRestoreInstanceState(SparseArray<Parcelable> container) {
        // Makes sure that the state of the child views in the side
        // spinner are not restored since we handle the state in the
        // onSaveInstanceState.
        super.dispatchThawSelfOnly(container);
    }

    public void setVolume(int volume) {
        this.volume = volume;
    }

    public void setIsMuted(boolean isMuted) {
        this.isMuted = isMuted;
    }

    public void setName(String name) {
        this.name = name;
    }
}
